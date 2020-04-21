﻿using Helper;
using SqlKata;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    public class AccommodationHelper
    {
        public List<string> boardlist;
        public List<string> categorylist;
        public List<string> accotypelist;
        public Dictionary<string, bool> featurelist;
        public Dictionary<string, bool> themelist;
        public List<string> badgelist;
        public List<string> idlist;
        public List<string> smgtaglist;
        public List<string> districtlist;
        public List<string> municipalitylist;
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public bool apartment;
        public bool? active;
        public bool? smgactive;
        public string? lastchange;

        public static async Task<AccommodationHelper> CreateAsync(
            QueryFactory queryFactory, string? idfilter, string? locfilter, 
            string? boardfilter, string? categoryfilter, string? typefilter, string? featurefilter,
            string? badgefilter, string? themefilter, string? smgtags,
            bool? activefilter, bool? smgactivefilter, string? lastchange,
            CancellationToken cancellationToken)
        {
            IEnumerable<string>? tourismusvereinids = null;
            if (locfilter != null && locfilter.Contains("mta"))
            {
                List<string> metaregionlist = CommonListCreator.CreateDistrictIdList(locfilter, "mta");
                tourismusvereinids = await GenericHelper.RetrieveLocFilterDataAsync(queryFactory, metaregionlist, cancellationToken);
            }

            return new AccommodationHelper(
                idfilter: idfilter, locfilter: locfilter, boardfilter: boardfilter, categoryfilter: categoryfilter, typefilter: typefilter,
                featurefilter: featurefilter, badgefilter: badgefilter, themefilter: themefilter,
                activefilter: activefilter, smgactivefilter: smgactivefilter, smgtags: smgtags, lastchange: lastchange,
                tourismusvereinids: tourismusvereinids);
        }

        private AccommodationHelper(
            string? idfilter, string? locfilter, string? boardfilter, string? categoryfilter, string? typefilter, 
            string? featurefilter, string? badgefilter, string? themefilter,
            bool? activefilter, bool? smgactivefilter, string? smgtags, string? lastchange,
            IEnumerable<string>? tourismusvereinids)
        {

            boardlist = AccoListCreator.CreateBoardListFromFlag(boardfilter);
            categorylist = AccoListCreator.CreateCategoryListfromFlag(categoryfilter);
            accotypelist = AccoListCreator.CreateAccoTypeListfromFlag(typefilter);
            featurelist = AccoListCreator.CreateFeatureListDictfromFlag(featurefilter);
            badgelist = AccoListCreator.CreateBadgeListfromFlag(badgefilter);
            themelist = AccoListCreator.CreateThemeListDictfromFlag(themefilter);
            idlist = String.IsNullOrEmpty(idfilter) ? new List<string>() : CommonListCreator.CreateIdList(idfilter.ToUpper());            

            tourismvereinlist = new List<string>();
            regionlist = new List<string>();
            if (locfilter != null && locfilter.Contains("reg"))
                regionlist = CommonListCreator.CreateDistrictIdList(locfilter, "reg");
            if (locfilter != null && locfilter.Contains("tvs"))
                tourismvereinlist = CommonListCreator.CreateDistrictIdList(locfilter, "tvs");
            if (locfilter != null && locfilter.Contains("mun"))
                municipalitylist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "mun");
            if (locfilter != null && locfilter.Contains("fra"))
                districtlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "fra");

            if (tourismusvereinids != null)
                tourismvereinlist.AddRange(tourismusvereinids);

            ///Special Case Apartment Setting Filter to HasApartment
            apartment = false;
            if (accotypelist.Contains("Apartment"))
            {
                apartment = true;
                accotypelist.Remove("Apartment");
            }
            
            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;
        }

       
    }
}