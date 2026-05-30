namespace accs.Logging
{
    public static class EventIds
    {
		// Information

		public const int Details = 100;
		public const int NoData = 101;
		public const int Processing = 102;
		public const int Saving = 103;

		// Successed

		public const int Ok = 200;
		public const int Created = 201;
		public const int Read = 202;
		public const int Updated = 203;
		public const int Deleted = 204;
		public const int Accessed = 205;

		// User Errors

		public const int BadData = 300;
		public const int Unauthorized = 301;
		public const int InvalidData = 302;
		public const int Forbidden = 303;
		public const int NotFound = 304;

		// System Errors

		public const int UnhandledError = 400;
		public const int NotImplemented = 401;
		public const int ExternalError = 402;
		public const int ConnectionError = 403;
	}
}
