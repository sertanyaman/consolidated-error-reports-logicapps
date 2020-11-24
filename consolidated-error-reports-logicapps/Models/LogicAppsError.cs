namespace consolidated_error_reports_logicapps.Models
{
    /// <summary>
    /// Consodilated error message format for logic app errors
    /// </summary>
    class LogicAppsError
    {
        public string Code { get; set; }
        public string BlockName { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string ClientTrackingId { get; set; }
        public string StartTime { get; set; }
    }

}
