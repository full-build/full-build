﻿module Paket.PlatformMatching

open System

[<Literal>]
let MaxPenalty = 1000000

let inline split (path : string) = 
    path.Split('+')
    |> Array.map (fun s -> s.Replace("portable-", ""))
    
let extractPlatforms = Collections.memoize (fun path  -> split path |> Array.choose FrameworkDetection.Extract |> Array.toList)

let knownInPortable =
  KnownTargetProfiles.AllPortableProfiles 
  |> List.collect snd
  |> List.distinct

let extractAndTryGetProfile = Collections.memoize (fun path ->
    let platforms = extractPlatforms path
    let filtered =
      platforms
      |> List.filter (fun p -> knownInPortable |> Seq.exists ((=) p))
      |> List.sort

    KnownTargetProfiles.AllPortableProfiles |> Seq.tryFind (snd >> List.sort >> (=) filtered)
    |> Option.map PortableProfile)

let getPlatformPenalty =
    let rec getPlatformPenalty alreadyChecked (targetPlatform:FrameworkIdentifier) (packagePlatform:FrameworkIdentifier) =
        if packagePlatform = targetPlatform then
            0
        else
            let penalty =
                targetPlatform.SupportedPlatforms
                |> List.filter (fun x -> Set.contains x alreadyChecked |> not)
                |> List.map (fun target -> getPlatformPenalty (Set.add target alreadyChecked) target packagePlatform)
                |> List.append [MaxPenalty]
                |> List.min
                |> fun p -> p + 1

            match targetPlatform, packagePlatform with
            | DotNetFramework _, DotNetStandard _ -> 200 + penalty
            | DotNetStandard _, DotNetFramework _ -> 200 + penalty
            | _ -> penalty

    Collections.memoize (fun (targetPlatform:FrameworkIdentifier,packagePlatform:FrameworkIdentifier) -> getPlatformPenalty Set.empty targetPlatform packagePlatform)

let getPathPenalty =
    Collections.memoize 
      (fun (path:string,platform:FrameworkIdentifier) ->
        if String.IsNullOrWhiteSpace path then
            match platform with
            | Native(_) -> MaxPenalty // an empty path is considered incompatible with native targets            
            | _ -> 500 // an empty path is considered compatible with every .NET target, but with a high penalty so explicit paths are preferred
        else
            extractPlatforms path
            |> List.map (fun target -> getPlatformPenalty(platform,target))
            |> List.append [ MaxPenalty ]
            |> List.min)

// Checks wether a list of target platforms is supported by this path and with which penalty. 
let getPenalty (requiredPlatforms:FrameworkIdentifier list) (path:string) =
    requiredPlatforms
    |> List.sumBy (fun p -> getPathPenalty(path,p))

type PathPenalty = (string * int)

let comparePaths (p1 : PathPenalty) (p2 : PathPenalty) =
    let platformCount1 = (extractPlatforms (fst p1)).Length
    let platformCount2 = (extractPlatforms (fst p2)).Length

    // prefer full framework over portable
    if platformCount1 = 1 && platformCount2 > 1 then
        -1
    else if platformCount1 > 1 && platformCount2 = 1 then
        1
    // prefer lower version penalty
    else if snd p1 < snd p2 then
       -1
    else if snd p1 > snd p2 then
       1
    // prefer portable platform whith less platforms
    else if platformCount1 < platformCount2 then
        -1
    else if platformCount1 > platformCount2 then
        1
    else
        0

let platformsSupport = 
    let rec platformsSupport platform platforms = 
        if List.isEmpty platforms then MaxPenalty
        elif platforms |> List.exists ((=) platform) then 1
        else 
            platforms
            |> List.collect (fun (p : FrameworkIdentifier) -> 
                    KnownTargetProfiles.AllProfiles
                    |> List.choose (function 
                            | SinglePlatform f -> Some f
                            | _ -> None)
                    |> List.filter (fun f -> f.SupportedPlatforms |> List.exists ((=) p)))
            |> platformsSupport platform
            |> (+) 1

    Collections.memoize (fun (platform,platforms) -> platformsSupport platform platforms)


let findBestMatch = 
    let rec findBestMatch (paths : string list,targetProfile : TargetProfile) = 
        let requiredPlatforms = 
            match targetProfile with
            | PortableProfile(_, platforms) -> platforms
            | SinglePlatform(platform) -> [ platform ]

        let supported =
            paths 
            |> List.map (fun path -> path, (getPenalty requiredPlatforms path))
            |> List.filter (fun (_, penalty) -> penalty < MaxPenalty)
            |> List.sortWith comparePaths
            |> List.map fst
            |> List.tryHead

        let findBestPortableMatch findPenalty (portableProfile:TargetProfile) paths =
            paths
            |> Seq.tryFind (fun p -> extractAndTryGetProfile p = Some portableProfile)
            |> Option.map (fun p -> p, findPenalty)

        match supported with
        | None ->
            // Fallback Portable Library
            KnownTargetProfiles.AllProfiles
            |> List.choose (fun p ->
                match targetProfile with
                | SinglePlatform x ->
                    match platformsSupport(x,p.ProfilesCompatibleWithPortableProfile) with
                    | pen when pen < MaxPenalty ->
                        findBestPortableMatch pen p paths
                    | _ -> 
                        None
                | _ -> None)
            |> List.distinct
            |> List.sortBy (fun (x, pen) -> pen, (extractPlatforms x).Length) // prefer portable platform whith less platforms
            |> List.map fst
            |> List.tryHead
        | path -> path

    Collections.memoize (fun (paths : string list,targetProfile : TargetProfile) -> findBestMatch(paths,targetProfile))

// For a given list of paths and target profiles return tuples of paths with their supported target profiles.
// Every target profile will only be listed for own path - the one that best supports it. 
let getSupportedTargetProfiles =    
    Collections.memoize 
        (fun (paths : string list) ->
            KnownTargetProfiles.AllProfiles
            |> List.choose (fun target ->
                match findBestMatch(paths,target) with
                | Some p -> Some(p, target)
                | _ -> None)
            |> List.groupBy fst
            |> List.map (fun (path, group) -> path, List.map snd group)
            |> Map.ofList)


let getTargetCondition (target:TargetProfile) =
    match target with
    | SinglePlatform(platform) -> 
        match platform with
        | DotNetFramework(version) when version = FrameworkVersion.V4_Client ->
            "$(TargetFrameworkIdentifier) == '.NETFramework'", sprintf "($(TargetFrameworkVersion) == '%O' And $(TargetFrameworkProfile) == 'Client')" version
        | DotNetFramework(version) ->"$(TargetFrameworkIdentifier) == '.NETFramework'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | DNX(version) ->"$(TargetFrameworkIdentifier) == 'DNX'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | DNXCore(version) ->"$(TargetFrameworkIdentifier) == 'DNXCore'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | DotNetStandard(version) ->"$(TargetFrameworkIdentifier) == '.NETStandard'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | DotNetCore(version) ->"$(TargetFrameworkIdentifier) == '.NETCoreApp'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | Windows(version) -> "$(TargetFrameworkIdentifier) == '.NETCore'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | Silverlight(version) -> "$(TargetFrameworkIdentifier) == 'Silverlight'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | WindowsPhoneApp(version) -> "$(TargetFrameworkIdentifier) == 'WindowsPhoneApp'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | WindowsPhoneSilverlight(version) -> "$(TargetFrameworkIdentifier) == 'WindowsPhone'", sprintf "$(TargetFrameworkVersion) == '%O'" version
        | MonoAndroid -> "$(TargetFrameworkIdentifier) == 'MonoAndroid'", ""
        | MonoTouch -> "$(TargetFrameworkIdentifier) == 'MonoTouch'", ""
        | MonoMac -> "$(TargetFrameworkIdentifier) == 'MonoMac'", ""
        | XamariniOS -> "$(TargetFrameworkIdentifier) == 'Xamarin.iOS'", ""
        | XamarinMac -> "$(TargetFrameworkIdentifier) == 'Xamarin.Mac'", ""
        | Native("","") -> "true", ""
        | Native("",bits) -> (sprintf "'$(Platform)'=='%s'" bits), ""
        | Runtimes(platform) -> failwithf "Runtime dependencies are unsupported in project files."
        | Native(profile,bits) -> (sprintf "'$(Configuration)|$(Platform)'=='%s|%s'" profile bits), ""
    | PortableProfile(name, _) -> sprintf "$(TargetFrameworkProfile) == '%O'" name,""

let getCondition (referenceCondition:string option) (allTargets: TargetProfile list list) (targets : TargetProfile list) =
    let inline CheckIfFullyInGroup typeName matchF (processed,targets) =
        let fullyContained = 
            KnownTargetProfiles.AllDotNetProfiles 
            |> List.filter matchF
            |> List.forall (fun p -> targets |> Seq.exists ((=) p))

        if fullyContained then
            (sprintf "$(TargetFrameworkIdentifier) == '%s'" typeName,"") :: processed,targets |> List.filter (matchF >> not)
        else
            processed,targets

    let grouped,targets =
        ([],targets)
        |> CheckIfFullyInGroup "true" (fun _ -> true)
        |> CheckIfFullyInGroup ".NETFramework" (fun x -> match x with | SinglePlatform(DotNetFramework(_)) -> true | _ -> false)
        |> CheckIfFullyInGroup ".NETCore" (fun x -> match x with | SinglePlatform(Windows(_)) -> true | _ -> false)
        |> CheckIfFullyInGroup "Silverlight" (fun x -> match x with |SinglePlatform(Silverlight(_)) -> true | _ -> false)
        |> CheckIfFullyInGroup "WindowsPhoneApp" (fun x -> match x with | SinglePlatform(WindowsPhoneApp(_)) -> true | _ -> false)
        |> CheckIfFullyInGroup "WindowsPhone" (fun x -> match x with | SinglePlatform(WindowsPhoneSilverlight(_)) -> true | _ -> false)

    let targets =
        targets 
        |> List.map (fun target ->
            match target with
            | SinglePlatform(DotNetFramework(FrameworkVersion.V4_Client)) ->
                if allTargets |> List.exists (List.contains (SinglePlatform(DotNetFramework(FrameworkVersion.V4)))) |> not then
                    SinglePlatform(DotNetFramework(FrameworkVersion.V4))
                else
                    target
            | _ -> target)

    let conditions =
        if targets = [ SinglePlatform(Native("", "")) ] then 
            targets
        else 
            targets 
            |> List.filter (function
                           | SinglePlatform(Native("", "")) -> false
                           | SinglePlatform(Runtimes(_)) -> false
                           | SinglePlatform(DotNetFramework(FrameworkVersion.V4_Client)) ->
                                targets |> List.contains (SinglePlatform(DotNetFramework(FrameworkVersion.V4))) |> not
                           | _ -> true)
        |> List.map getTargetCondition
        |> List.filter (fun (_, v) -> v <> "false")
        |> List.append grouped
        |> List.groupBy fst

    let conditionString =
        let andString = 
            conditions
            |> List.map (fun (group,conditions) ->
                match List.ofSeq (conditions |> Seq.map snd |> Set.ofSeq) with
                | [ "" ] -> group
                | [] -> "false"
                | [ detail ] -> sprintf "%s And %s" group detail
                | conditions ->
                    let detail =
                        conditions
                        |> fun cs -> String.Join(" Or ",cs)
                        
                    sprintf "%s And (%s)" group detail)
        
        match andString with
        | [] -> ""
        | [x] -> x
        | xs -> String.Join(" Or ", List.map (fun cs -> sprintf "(%s)" cs) xs)
    
    match referenceCondition with 
    | None -> conditionString
    | Some condition ->
        // msbuild triggers a warning MSB4130 when we leave out the quotes around the condition
        // and add the condition at the end
        if conditionString = "$(TargetFrameworkIdentifier) == 'true'" || String.IsNullOrWhiteSpace conditionString then
            sprintf "'$(%s)' == 'True'" condition
        else
            sprintf "'$(%s)' == 'True' And (%s)" condition conditionString