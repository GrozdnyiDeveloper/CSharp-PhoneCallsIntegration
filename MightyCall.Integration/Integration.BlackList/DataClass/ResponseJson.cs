namespace crmPark.Integration.External.DataClass
{
    public class ResponseJson
    {
        public bool Result;
        public string Message;
        public ResponseJson()
        {

        }
        public ResponseJson(bool Result, string Message)
        {
            this.Result = Result;
            this.Message = Message;
        }
    }
}