﻿using Helper;
using SqlKata;
using SqlKata.Execution;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    public class CommonHelper
    {
        public List<string> idlist;
        public List<string> languagelist;
        public List<string> smgtaglist;
        public bool? visibleinsearch;
        public string? lastchange;
        public bool? active;
        public bool? smgactive;
        //New Publishedonlist
        public List<string> publishedonlist;

        public static Task<CommonHelper> CreateAsync(
            QueryFactory queryFactory, string? idfilter, string? languagefilter, bool? visibleinsearch, 
            bool? activefilter, bool? smgactivefilter, string? smgtags, string? lastchange, string? publishedonfilter,
            CancellationToken cancellationToken)
        {           
            return Task.FromResult(new CommonHelper(
               idfilter: idfilter, languagefilter: languagefilter,
                activefilter: activefilter, smgactivefilter: smgactivefilter, visibleinsearch: visibleinsearch, 
                smgtags: smgtags, lastchange: lastchange, publishedonfilter: publishedonfilter));
        }

        private CommonHelper(
            string? idfilter, string? languagefilter, bool? activefilter, bool? smgactivefilter, bool? visibleinsearch, 
            string? smgtags, string? lastchange, string? publishedonfilter)
        {           
            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());

            languagelist = Helper.CommonListCreator.CreateIdList(languagefilter?.ToLower());


            smgtaglist = CommonListCreator.CreateIdList(smgtags);
            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            //smgactive
            this.visibleinsearch = visibleinsearch;

            this.lastchange = lastchange;

            publishedonlist = Helper.CommonListCreator.CreateIdList(publishedonfilter?.ToLower());
        }


    }
}
