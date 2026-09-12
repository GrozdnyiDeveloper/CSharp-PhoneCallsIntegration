using System;

namespace crmPark.Integration.External.DataClass
{
    public class TerminateRequestModel
    {
        public string PhoneNumber { get; set; }
        public Guid? Campaingid { get; set; }
    }
}