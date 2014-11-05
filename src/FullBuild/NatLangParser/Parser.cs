using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FullBuild.NatLangParser
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
            var res = this.Aggregate(new StringBuilder().Append("\t"), (sb, m) => sb.AppendLine(m.Usage()).Append("\t"));
            return res.ToString();
        }
    }
}
