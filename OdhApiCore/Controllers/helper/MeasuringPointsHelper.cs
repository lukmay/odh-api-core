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
    public class MeasuringPointsHelper
    {
        public List<string> idlist;
        public List<string> districtlist;
        public List<string> municipalitylist;
        public List<string> tourismvereinlist;
        public List<string> regionlist;
        public List<string> arealist;
        public bool? active;
        public bool? smgactive;
        public string? lastchange;

        public static async Task<MeasuringPointsHelper> Create(
            QueryFactory queryFactory, string? idfilter, string? locfilter, string? areafilter, string? skiareafilter, bool? activefilter, bool? smgactivefilter, string? lastchange,
            CancellationToken cancellationToken)
        {
            //TODO SKIAREAFILTER AND AREAFILTER not like on Activity Endpoint!

            var arealistfromskiarea = await GenericHelper.RetrieveAreaFilterDataAsync(queryFactory, skiareafilter, cancellationToken);
            var arealistfromarea = await GenericHelper.RetrieveAreaFilterDataAsync(queryFactory, areafilter, cancellationToken);

            IEnumerable<string>? tourismusvereinids = null;
            if (locfilter != null && locfilter.Contains("mta"))
            {
                List<string> metaregionlist = CommonListCreator.CreateDistrictIdList(locfilter, "mta");
                tourismusvereinids = await GenericHelper.RetrieveLocFilterDataAsync(queryFactory, metaregionlist, cancellationToken);
            }

            return new MeasuringPointsHelper(
                idfilter: idfilter, locfilter: locfilter, areafilterlist: arealistfromarea, skiareafilterlist: arealistfromskiarea,
                activefilter: activefilter, smgactivefilter: smgactivefilter,
                lastchange: lastchange, tourismusvereinids: tourismusvereinids);
        }

        private MeasuringPointsHelper(
            string? idfilter, string? locfilter, IEnumerable<string> areafilterlist, IEnumerable<string> skiareafilterlist, bool? activefilter, bool? smgactivefilter, string? lastchange, IEnumerable<string>? tourismusvereinids)
        {
            idlist = Helper.CommonListCreator.CreateIdList(idfilter?.ToUpper());

            arealist = new List<string>();

            if (areafilterlist != null)
                arealist.AddRange(areafilterlist);
            if (skiareafilterlist != null)
                arealist.AddRange(skiareafilterlist);

            tourismvereinlist = new List<string>();
            regionlist = new List<string>();
            municipalitylist = new List<string>();
            districtlist = new List<string>();

            if (locfilter != null && locfilter.Contains("reg"))
                regionlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "reg");
            if (locfilter != null && locfilter.Contains("tvs"))
                tourismvereinlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "tvs");
            if (locfilter != null && locfilter.Contains("mun"))
                municipalitylist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "mun");
            if (locfilter != null && locfilter.Contains("fra"))
                districtlist = Helper.CommonListCreator.CreateDistrictIdList(locfilter, "fra");

            if (tourismusvereinids != null)
                tourismvereinlist.AddRange(tourismusvereinids);

            //active
            active = activefilter;

            //smgactive
            smgactive = smgactivefilter;

            this.lastchange = lastchange;
        }


    }
}