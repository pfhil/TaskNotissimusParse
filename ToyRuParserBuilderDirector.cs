using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskNotissimusParse
{
    public class ToyRuParserBuilderDirector
    {
        private readonly ToyRuParserBuilder _builder;

        public ToyRuParserBuilderDirector(ToyRuParserBuilder builder)
        {
            _builder = builder;
        }

        public void Build(AvailableParsingRegionsEnum region)
        {
            _builder.SetRequestDelay();
            _builder.SetRequestTimeOut();
            _builder.SetProxy();
            _builder.SetBrowsingContext();
            _builder.SetParsingRegion(region);
            _builder.SetHttpClientHandler();
            _builder.SetHttpClient();
            _builder.SetWorkIfException();
            _builder.SetHtmlDownloader();
        }
    }
}
