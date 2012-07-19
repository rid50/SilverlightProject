using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using NHibernate.Type;
using NHibernate.SqlCommand;
using NHibernate.Linq;

using FaihaProject.Dal.Entities;
using FaihaProject.Dal.Dto;

#pragma warning disable 219


namespace FaihaProject.Dal.Repositories
{
    public class PaymentRepository
    {
        public ICollection<PaymentPenaltyDto> DLookUpPaymentPenalty(string contractID, short paymentNo)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                string sql = @" SELECT distinct 
                        new TotalPayment(wo.ContractID, wo.WOID, wo.WOID,
                                        sum(wod.RealQuantity * wod.UnitPrice), 0m)
                        FROM WorkOrder wo, WorkOrderDetail wod, PaymentDetail p
	                    WHERE (wo.HandOverWODate is not null) AND
                             (wo.ContractID = wod.ContractID AND wo.WOID = wod.WOID) AND
                             (wo.ContractID = p.ContractID AND wo.WOID = p.WOID) AND
                             (p.ContractID = '" + contractID + "' And p.PaymentNo = " + paymentNo + ")" +
                        " GROUP BY wo.ContractID, wo.WOID";


                var totalPayment = session.CreateQuery(sql)
                    //.SetMaxResults(10)
                    .List<TotalPayment>();

                //return totalPayment;

                sql = @" SELECT distinct new PaymentDto(wo.ContractID, p.PaymentNo, wo.WOID, 0m, wo.Discount,
                                wo.HandOverWODate, wo.WODate, wo.WOAge, wo.Contract.ContractValue, wo.WODate)
                        FROM WorkOrder wo, PaymentDetail p
                        WHERE (wo.ContractID = p.ContractID AND wo.WOID = p.WOID) AND
                            (p.ContractID = '" + contractID + "' And p.PaymentNo = " + paymentNo + ")";

                ICollection<PaymentDto> paymentDto = session.CreateQuery(sql)
                    //.SetMaxResults(10)
                    .List<PaymentDto>();

                //Console.WriteLine("Total records: {0}", paymentDto.Count());

                foreach (TotalPayment t in totalPayment)
                {
                    foreach (PaymentDto p in paymentDto)
                    {
                        if (t.ContractID == p.ContractID && t.WOID == p.WOID)
                        {
                            t.PaymentNo = p.PaymentNo;
                            t.Discount = p.Discount;
                            //t.Delay = p.Delay;
                            t.WOAge = p.WOAge;
                            t.HandOverWODate = p.HandOverWODate;
                            t.WODate = p.WODate;
                            t.ContractValue = p.ContractValue;
                            //Console.WriteLine("Delay: {0}, Age: {1}, Value: {2}", p.Delay, p.WOAge, p.ContractValue);
                            break;
                        }
                    }
                }

                //return totalPayment;

                var total =
                        from td in totalPayment
                        where td.ContractID == contractID && td.PaymentNo == paymentNo
                        group td by new { td.ContractID, td.PaymentNo } into grouping
                        select new
                        {
                            grouping.Key,
                            total = grouping.Sum(t => t.Total)
                        };

                var penalty =
                        from td in totalPayment
                        where td.ContractID == contractID && td.PaymentNo == paymentNo
                        group td by new { td.ContractID, td.PaymentNo } into grouping
                        select new
                        {
                            grouping.Key,
                            penalty = grouping.Sum(t => getPenalty(t.ContractValue, t.WOAge, t.Delay, t.Total))
                        };

                ICollection<PaymentPenaltyDto> dto = new List<PaymentPenaltyDto>();
                PaymentPenaltyDto pp = new PaymentPenaltyDto();
                foreach (var grp in total)
                {
                    pp.TotalValue = decimal.Round(grp.total, 3);
                    break;
                }

                foreach (var grp in penalty)
                {
                    pp.Penalty = decimal.Round(grp.penalty, 3);
                    break;
                }

                dto.Add(pp);

                return dto;
            }
        }


        private decimal getPenalty(decimal? contractValue, int? contractAge, int? delay, decimal? total)
        {
            if (!(contractValue == null || contractAge == null || delay == null || total == null)) {
                if (delay > 0) {
                    if (total * 0.25m * delay / contractAge >= contractValue * 0.1m)
                        return (decimal)contractValue * 0.1m;
                    else
                        return (decimal)(total * 0.25m * delay / contractAge);
                } else
                    return 0;
            } else
                return 0;
        }

        public ICollection<PaymentDto> GetPayment()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var ic = session.CreateCriteria(typeof(Contractor))
                    //.SetMaxResults(10)
                    .CreateCriteria("Contracts", "c")
                    .CreateAlias("Payments", "p")
                    .SetProjection(Projections.Distinct(Projections.ProjectionList()
                        .Add(Projections.Property("c.ContractID"), "ContractID")
                        //.Add(Projections.Property("p.PaymentNo"), "PaymentNo")
                        .Add(Projections.Property("c.ClassID"), "ClassID")
                        .Add(Projections.Property<Contractor>(c => c.ContractorName).As("ContractorName"))
                        .Add(Projections.Property("c.ContractTitle"), "ContractTitle")
                        .Add(Projections.Property("c.TenderTitle"), "TenderTitle")
                        .Add(Projections.Property("p.PaymentDate"), "PaymentDate")
                        .Add(Projections.Property("p.Description"), "Description")
                        .Add(Projections.Property("c.ContractValue"), "ContractValue")
                        .Add(Projections.Property("c.ContractPeriod"), "ContractPeriod")
                        .Add(Projections.Property("c.ContractStartDate"), "ContractStartDate")
                        .Add(Projections.Property("c.ContractEndDate"), "ContractEndDate")
                        .Add(Projections.Property("c.ExtensionEndDate"), "ExtensionEndDate")

                    ))
                    .SetResultTransformer(new AliasToBeanResultTransformer(typeof(PaymentDto)))
                    .List<PaymentDto>();
                return ic;
            }
        }

        public ICollection<PaymentDetailDto> GetPaymentDetail(string contractID)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
//                var ic = session.CreateCriteria(typeof(Contractor))
                var ic = session.CreateCriteria(typeof(Payment), "p")
                    //.SetMaxResults(10)
                    //.CreateCriteria("Contracts", "c")
                    //.CreateCriteria("Payments", "p")
                    .Add(Restrictions.Eq("p.ContractID", contractID))
                    .SetProjection(Projections.Distinct(Projections.ProjectionList()
                        .Add(Projections.Property("p.ContractID"), "ContractID")
                        .Add(Projections.Property("p.PaymentNo"), "PaymentNo")
                        .Add(Projections.Property("p.D6"), "D6")
                        .Add(Projections.Property("p.D7"), "D7")
                        .Add(Projections.Property("p.D8"), "D8")
                        .Add(Projections.Property("p.C9"), "C9")
                        .Add(Projections.Property("p.I10"), "I10")
                    //                        .Add(Projections.SqlProjection(
                    //                                "=DLookUp(\"[Total]\",\"q_payment_total_for_accounting\"," +
                    //                                        "\"[Contract No] = '\" & p2_.[Contract No] & \"' And [Payment No] = p2_.[Payment No]\")",
                    //                                new string[] { "C11" }, new IType[] { NHibernateUtil.Currency }), "C11")

                        .Add(Projections.Property("p.C12"), "C12")
                        .Add(Projections.Property("p.C13"), "C13")
                        .Add(Projections.Property("p.C14"), "C14")
                        .Add(Projections.Property("p.C15"), "C15")
                        .Add(Projections.Property("p.C16"), "C16")
                        .Add(Projections.Property("p.C18"), "C18")
                        .Add(Projections.Property("p.C20"), "C20")
                        .Add(Projections.Property("p.C21"), "C21")
                        .Add(Projections.Property("p.C22"), "C22")
                        .Add(Projections.Property("p.C23"), "C23")
                        .Add(Projections.Property("p.C24"), "C24")
                    ))
                    .SetResultTransformer(new AliasToBeanResultTransformer(typeof(PaymentDetailDto)))
                    .List<PaymentDetailDto>();
                return ic;
            }
        }

    }
}