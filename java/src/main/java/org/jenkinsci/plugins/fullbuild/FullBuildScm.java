package org.jenkinsci.plugins.fullbuild;

import java.io.*;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;
import java.util.logging.Level;
import java.util.logging.Logger;

import hudson.EnvVars;
import hudson.Extension;
import hudson.FilePath;
import hudson.Launcher;
import hudson.Util;
import hudson.model.Job;
import hudson.model.ParameterDefinition;
import hudson.model.ParametersDefinitionProperty;
import hudson.model.Run;
import hudson.model.StringParameterDefinition;
import hudson.model.TaskListener;
import hudson.scm.ChangeLogParser;
import hudson.scm.SCM;
import hudson.scm.SCMDescriptor;
import hudson.scm.SCMRevisionState;
import net.sf.json.JSONObject;

import org.apache.commons.lang.StringUtils;
import org.kohsuke.stapler.DataBoundConstructor;
import org.kohsuke.stapler.DataBoundSetter;
import org.kohsuke.stapler.QueryParameter;
import org.kohsuke.stapler.StaplerRequest;
import org.kohsuke.stapler.export.Exported;
import org.kohsuke.stapler.export.ExportedBean;

import hudson.util.FormValidation;

import javax.annotation.CheckForNull;
import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * The main entrypoint of the plugin. This class contains code to store user
 * configuration and to check out the code using a repo binary.
 */

@ExportedBean
public class FullBuildScm extends SCM implements Serializable {

	private static Logger debug = Logger
			.getLogger("hudson.plugins.full-build.FullBuildScm");

	// Advanced Fields:
	@CheckForNull private String masterRepository;
	@CheckForNull private String protocol;
	@CheckForNull private boolean shallow;
	@CheckForNull private String branch;
    @CheckForNull private Set<String> repositories;

	/**
	 * Returns the manifest branch name. By default, this is null and repo
	 * defaults to "master".
	 */
	@Exported
	public String getBranch() {
		return branch;
	}

    @DataBoundSetter
    public void setBranch(String branch) { this.branch = branch; }

	/**
	 * Returns the repo url. by default, this is null and
	 * repo is fetched from aosp
	 */
	@Exported
	public String getMasterRepository() {
		return masterRepository;
	}

    @DataBoundSetter
    public void setMasterRepository(String masterRepository) { this.masterRepository = masterRepository; }

	/**
	 * Returns the name of the mirror directory. By default, this is null and
	 * repo does not use a mirror.
	 */
	@Exported
	public String getProtocol() {
		return protocol;
	}

    @DataBoundSetter
    public void setProtocol(String protocol) { this.protocol = protocol; }

	/**ß
	 * Returns the depth used for sync.  By default, this is null and repo
	 * will sync the entire history.
	 */
	@Exported
	public boolean getShallow() {
		return this.shallow;
	}

    @DataBoundSetter
    public void setShallow(boolean shallow) { this.shallow = shallow; }

    /**
     * returns list of ignore projects.
     */
    @Exported
    public String getIgnoreProjects() {
        return StringUtils.join(repositories, '\n');
    }

    @DataBoundSetter
    public final void setIgnoreProjects(final String repositories) {
        if (repositories == null) {
            this.repositories = Collections.emptySet();
            return;
        }
        this.repositories = new LinkedHashSet<>(
                Arrays.asList(repositories.split("\\s+")));
    }

    @Override
    public ChangeLogParser createChangeLogParser() {
        return new ChangeLog();
    }
	/**
	 * The constructor takes in user parameters and sets them. Each job using
	 * the FullBuildScm will call this constructor.
	 *
	 * @param masterRepository The URL for the manifest repository.
	 */
	@DataBoundConstructor
	public FullBuildScm(final String masterRepository) {
		this.masterRepository = masterRepository;
		this.protocol = "gerrit";
		this.branch = null;
		this.shallow = true;
        this.repositories = Collections.emptySet();
	}

	@Override
	public SCMRevisionState calcRevisionsFromBuild(
			@Nonnull final Run<?, ?> build, @Nullable final FilePath workspace,
			@Nullable final Launcher launcher, @Nonnull final TaskListener listener
			) throws IOException, InterruptedException {
        // do not really care - we will always build
		return null;
	}

	/**
	 * @param environment   an existing environment, which contains already
	 *                      properties from the current build
	 * @param project       the project that is being built
	 */
	private EnvVars getEnvVars(final EnvVars environment,
							   final Job<?, ?> project) {
		// create an empty vars map
		final EnvVars finalEnv = new EnvVars();
		final ParametersDefinitionProperty params = project.getProperty(
				ParametersDefinitionProperty.class);
		if (params != null) {
			for (ParameterDefinition param
					: params.getParameterDefinitions()) {
				if (param instanceof StringParameterDefinition) {
					final StringParameterDefinition stpd =
							(StringParameterDefinition) param;
					final String dflt = stpd.getDefaultValue();
					if (dflt != null) {
						finalEnv.put(param.getName(), dflt);
					}
				}
			}
		}
		// now merge the settings from the last build environment
		if (environment != null) {
			finalEnv.overrideAll(environment);
		}

		EnvVars.resolve(finalEnv);
		return finalEnv;
	}

	@Override
	public void checkout(
			@Nonnull final Run<?, ?> build, @Nonnull final Launcher launcher,
			@Nonnull final FilePath workspace, @Nonnull final TaskListener listener,
			@CheckForNull final File changelogFile, @CheckForNull final SCMRevisionState baseline)
			throws IOException, InterruptedException {

		FilePath repoDir = workspace;

		Job<?, ?> job = build.getParent();
		EnvVars env = build.getEnvironment(listener);
		env = getEnvVars(env, job);
		if (!checkoutCode(launcher, repoDir, env, listener.getLogger(), changelogFile)) {
            throw new IOException("Could not init workspace");
        }
	}


	final private boolean checkoutCode(
            final Launcher launcher,
            final FilePath workspace,
            final EnvVars env,
            final OutputStream logger,
            File changelogFile)
			throws IOException, InterruptedException {

        final List<String> commands = new ArrayList<>(4);

		// init workspace first
        debug.log(Level.INFO, "Initializing workspace in: " + workspace.getName());
        commands.clear();
		commands.add(getDescriptor().getExecutable());
		commands.add("init");
		commands.add(env.expand(this.protocol));
		commands.add(env.expand(this.masterRepository));
		commands.add(workspace.getRemote());
		int initRetCode =
				launcher.launch().stdout(logger).pwd(workspace)
						.cmds(commands).envs(env).join();
		if (initRetCode != 0) {
			return false;
		}

		// install package
        debug.log(Level.INFO, "Installing packages");
        commands.clear();
        commands.add(getDescriptor().getExecutable());
        commands.add("install");
		int installRetCode =
				launcher.launch().stdout(logger).pwd(workspace)
						.cmds(commands).envs(env).join();
		if (installRetCode != 0) {
			return false;
		}

        // clone repositories
        debug.log(Level.INFO, "Cloning repositories");
        commands.clear();
        commands.add(getDescriptor().getExecutable());
        commands.add("clone");
        if(shallow)
            commands.add("--shallow");
        commands.add(env.expand(StringUtils.join(repositories, ' ')));
        int cloneRetCode =
                launcher.launch().stdout(logger).pwd(workspace)
                        .cmds(commands).envs(env).join();
        if (cloneRetCode != 0) {
            return false;
        }

        // get changes
        if(null != changelogFile) {
            debug.log(Level.INFO, "Getting changes");
            commands.clear();
            commands.add(getDescriptor().getExecutable());
            commands.add("history");
            ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
            int changeRetCode =
                    launcher.launch().stdout(byteArrayOutputStream).pwd(workspace)
                            .cmds(commands).envs(env).join();
            if (changeRetCode != 0) {
                return false;
            }

            try (OutputStream outputStream = new FileOutputStream(changelogFile)) {
                byteArrayOutputStream.writeTo(outputStream);

            }
        }

        return true;
	}

    /*
	@Override
	public ChangeLogParser createChangeLogParser() {
		return new ChangeLog();
	}
	*/

	@Override
	public DescriptorImpl getDescriptor() {
		return (DescriptorImpl) super.getDescriptor();
	}

	@Nonnull
	@Override
	public String getKey() {
		return new StringBuilder("full-build")
			.append(' ')
			.append(this.masterRepository)
			.toString();
	}

	/**
	 * A DescriptorImpl contains variables used server-wide. In our263 case, we
	 * only store the path to the fullbuild executable, which defaults to just
	 * "fullbuild". This class also handles some Jenkins housekeeping.
	 */
	@Extension
	public static class DescriptorImpl extends SCMDescriptor<FullBuildScm> {
		private String fbExecutable;

		/**
		 * Call the superclass constructor and load our configuration from the
		 * file system.
		 */
		public DescriptorImpl() {
			super(null);
			load();
		}

		@Override
		public String getDisplayName() {
			return "FullBuild";
		}

		@Override
		public boolean configure(final StaplerRequest req,
				final JSONObject json)
				throws hudson.model.Descriptor.FormException {
			fbExecutable =
					Util.fixEmptyAndTrim(json.getString("executable"));
			save();
			return super.configure(req, json);
		}

		/**
		 * Check that the specified parameter exists on the file system and is a
		 * valid executable.
		 *
		 * @param value
		 *            A path to an executable on the file system.
		 * @return Error if the file doesn't exist, otherwise return OK.
		 */
		public FormValidation doExecutableCheck(
				@QueryParameter final String value) {
			return FormValidation.validateExecutable(value);
		}

		/**
		 * Returns the command to use when running fullbuild. By default, we assume
		 * that repo is in the server's PATH and just return "fullbuild".
		 */
		public String getExecutable() {
			if (fbExecutable == null) {
				return "fullbuild";
			} else {
				return fbExecutable;
			}
		}

		@Override
		public boolean isApplicable(final Job project) {
			return true;
		}
	}
}
