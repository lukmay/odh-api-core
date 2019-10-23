﻿using Helper;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    public class ActivityHelper
    {
        public List<string> activitytypelist;
        public List<string> subtypelist;
        public List<string> idlist;
        public List<string> arealist;
        public List<string> smgtaglist;
        public List<string> difficultylist;
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public bool distance;
        public int distancemin;
        public int distancemax;
        public bool altitude;
        public int altitudemin;
        public int altitudemax;
        public bool duration;
        public int durationmin;
        public int durationmax;
        public bool? highlight;
        public bool? active;
        public bool? smgactive;
        public string? lastchange;

        public static async Task<ActivityHelper> CreateAsync(
            IPostGreSQLConnectionFactory connectionFactory, string? activitytype, string? subtypefilter, string? idfilter, string? locfilter,
            string? areafilter, string? distancefilter, string? altitudefilter, string? durationfilter,
            bool? highlightfilter, string? difficultyfilter, bool? activefilter, bool? smgactivefilter,
            string? smgtags, string? lastchange, CancellationToken cancellationToken)
        {
            var arealist = await GenericHelper.RetrieveAreaFilterDataAsync(connectionFactory, areafilter, cancellationToken);

            IEnumerable<string>? tourismusvereinids = null;
            if (locfilter != null && locfilter.Contains("mta"))
            {
                List<string> metaregionlist = CommonListCreator.CreateDistrictIdList(locfilter, "mta");
                tourismusvereinids = await GenericHelper.RetrieveLocFilterDataAsync(
                    connectionFactory, metaregionlist, cancellationToken).ToListAsync();
            }

            return new ActivityHelper(
                activitytype, subtypefilter, idfilter, locfilter, arealist, distancefilter, altitudefilter, durationfilter, highlightfilter, difficultyfilter, activefilter, smgactivefilter, smgtags, lastchange, tourismusvereinids);
        }

        private ActivityHelper(
            string? activitytype, string? subtypefilter, string? idfilter, string? locfilter,
            IEnumerable<string> arealist, string? distancefilter, string? altitudefilter, string? durationfilter,
            bool? highlightfilter, string? difficultyfilter, bool? activefilter, bool? smgactivefilter,
            string? smgtags, string? lastchange, IEnumerable<string>? tourismusvereinids)
        {
            activitytypelist = new List<string>();
            if (activitytype != null)
            {
                if (int.TryParse(activitytype, out int typeinteger))
                {
                    if (typeinteger != 1023)
                        activitytypelist = Helper.ActivityPoiListCreator.CreateActivityTypefromFlag(activitytype);
                }
                else
                {
                    activitytypelist.Add(activitytype);
                }
            }
            if (activitytypelist.Count > 0)
                subtypelist = Helper.ActivityPoiListCreator.CreateActivitySubTypefromFlag(
                    activitytypelist.FirstOrDefault(), subtypefilter);
            else
                subtypelist = new List<string>();

            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());

            this.arealist = arealist.ToList();

            smgtaglist = CommonListCreator.CreateIdList(smgtags);
            difficultylist = CommonListCreator.CreateDifficultyList(difficultyfilter, activitytype);

            tourismvereinlist = new List<string>();
            regionlist = new List<string>();
            if (locfilter != null && locfilter.Contains("reg"))
                regionlist = CommonListCreator.CreateDistrictIdList(locfilter, "reg");
            if (locfilter != null && locfilter.Contains("tvs"))
                tourismvereinlist = CommonListCreator.CreateDistrictIdList(locfilter, "tvs");

            if (tourismusvereinids != null)
                tourismvereinlist.AddRange(tourismusvereinids);        

            //Distance
            var (min, max) = CommonListCreator.CreateRangeString(distancefilter);
            distancemin = min * 1000;
            distancemax = max * 1000;

            //Altitude
            (altitudemin, altitudemax) = CommonListCreator.CreateRangeString(altitudefilter);

            //Duration
            (durationmin, durationmax) = CommonListCreator.CreateRangeString(durationfilter);

            //highlight
            highlight = highlightfilter;

            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;
        }

       
    }
}
