namespace accs.Logging
{
    public static class EventIds
    {
		// Information

		/// <summary>
		/// Подробности
		/// </summary>
		public const int Details = 100;
		/// <summary>
		/// Пустой объект
		/// </summary>
		public const int NoData = 101;
		/// <summary>
		/// Выполняется
		/// </summary>
		public const int Processing = 102;
		/// <summary>
		/// Сохранение в процессе
		/// </summary>
		public const int Saving = 103;

		// Successed

		/// <summary>
		/// Успешно
		/// </summary>
		public const int Ok = 200;
		/// <summary>
		/// Успешно создано
		/// </summary>
		public const int Created = 201;
		/// <summary>
		/// Успешно найдено и получено
		/// </summary>
		public const int Read = 202;
		/// <summary>
		/// Успешно обновлено
		/// </summary>
		public const int Updated = 203;
		/// <summary>
		/// Успешно удалено
		/// </summary>
		public const int Deleted = 204;
		/// <summary>
		/// Доступ успешно получен
		/// </summary>
		public const int Accessed = 205;

		// User Errors

		/// <summary>
		/// Не удалось считать входные данные
		/// </summary>
		public const int BadData = 300;
		/// <summary>
		/// Пользователь неавторизован
		/// </summary>
		public const int Unauthorized = 301;
		/// <summary>
		/// Неверные входные данные
		/// </summary>
		public const int InvalidData = 302;
		/// <summary>
		/// У пользователя недостаточно прав
		/// </summary>
		public const int Forbidden = 303;
		/// <summary>
		/// Не найдены нужные данные
		/// </summary>
		public const int NotFound = 304;
		/// <summary>
		/// Действие выполнить невозможно
		/// </summary>
		public const int ImpossibleAction = 305;

		// System Errors

		/// <summary>
		/// Произошла непредвиденная, но обработанная ошибка
		/// </summary>
		public const int HandledError = 400;
		/// <summary>
		/// Функционал не реализован
		/// </summary>
		public const int NotImplemented = 401;
		/// <summary>
		/// Произошла ошибка во внешних сервисах
		/// </summary>
		public const int ExternalError = 402;
		/// <summary>
		/// Произошла ошибка соединения
		/// </summary>
		public const int ConnectionError = 403;
		/// <summary>
		/// Произошла необработанная ошибка
		/// </summary>
		public const int FatalError = 404;
	}
}
