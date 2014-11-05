using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FullBuildInterface.NatLangParser
{
    public class Parser : List<Matcher>
    {
        public bool Parse(string[] args)
        {
            var res = this.Any(x => x.ParseAndInvoke(args));
            return res;
        }

        public string Usage()
        {
            var res = this.Aggregate(new StringBuilder(), (sb, m) => sb.AppendLine(m.Usage()));
            return res.ToString();
        }
    }
}
