namespace accs.Models.Enums
{
    public enum PermissionType
    {
		/// <summary>
		/// Все разрешения в одном и
		/// обход ограничений по работе 
		/// с вышестоящими должностями
		/// </summary>
		[Permission(
			Name = "Администратор",
			Description = "Все разрешения в одном и обход ограничений по работе с вышестоящими должностями"
			)]
        Administrator = 1,

		/// <summary>
		/// Боец не получает автоматические 
		/// выговора и благодарности
		/// за обязательные сборы
		/// </summary>
		[Permission(
			Name = "Освобождение от сборов",
			Description = "Боец не получает автоматические выговора и благодарности за обязательные сборы"
			)]
		ConstantBeggOf = 2,

		/// <summary>
		/// Разрешение регистрировать новых бойцов
		/// </summary>
		[Permission(
			Name = "Регистрация новых бойцов",
			Description = "Разрешение принимать новичков в клан"
			)]
		RegisterNewUnits = 3,

		/// <summary>
		/// Разрешение уволнять бойцов
		/// </summary>
		[Permission(
			Name = "Увольнение бойцов",
			Description = "Разрешение увольнять бойцов клана"
			)]
		DismissUnits = 4,

		/// <summary>
		/// Разрешение отправлять бойцов в отставку
		/// </summary>
		[Permission(
			Name = "Отправление отставку",
			Description = "Разрешение отправлять бойцов клана в отставку"
			)]
		AssignRetirement = 5,

		/// <summary>
		/// Разрешение на подтверждение своей и чужой активности
		/// </summary>
		[Permission(
			Name = "Подтверждение активности",
			Description = "Разрешение подтверждать фиксации своей и чужой активности"
			)]
		ConfirmActivity = 6,

		/// <summary>
		/// Разрешение на выход в отпуск
		/// </summary>
		[Permission(
			Name = "Выход в отпуск",
			Description = "Разрешение на выход в отпуск"
			)]
		VacationAccess = 7,

		/// <summary>
		/// Разрешение на выход в отставку
		/// </summary>
		[Permission(
			Name = "Выход в отставку",
			Description = "Разрешение на выход в отставку"
			)]
		AccessRetirement = 8,

		/// <summary>
		/// Возможность выдавать благодарности и выговора
		/// </summary>
		[Permission(
			Name = "Выдача статусов",
			Description = "Разрешение выдавать бойцам выговора, благодарности и строгие выговоры"
			)]
		GiveReprimandGratitude = 9,

		/// <summary>
		/// Возможность отправлять бойцов в отпуск
		/// </summary>
		[Permission(
			Name = "Отправка в отпуск",
			Description = "Разрешение отправлять бойцов в отпуск"
			)]
		ForceVacation = 10,

		/// <summary>
		/// Возможность присваивать звания бойцам
		/// </summary>
		[Permission(
			Name = "Присваивание званий",
			Description = "Разрешение изменять звания у бойцов"
			)]
		AssignRanks = 11,

		/// <summary>
		/// Возможность менять должности нижестоящих бойцов
		/// </summary>
		[Permission(
			Name = "Назначение на должности",
			Description = "Разрешение назначать нижестоящих бойцов на должности"
			)]
		AssignPosts = 12,

		/// <summary>
		/// Возможность награждать бойцов
		/// </summary>
		[Permission(
			Name = "Награждение бойцов",
			Description = "Разрешение награждать бойцов"
			)]
		AssignRewards = 13,

		/// <summary>
		/// Возможность редактировать звания
		/// </summary>
		[Permission(
			Name = "Редактирование званий",
			Description = "Разрешение создавать, удалять и редактировать звания"
			)]
		ManageRanks = 14,

		/// <summary>
		/// Возможность редактировать 
		/// нижестоящие должности и подразделения
		/// </summary>
		[Permission(
			Name = "Редактирование структуры",
			Description = "Разрешение создавать, удалять и редактировать нижестоящие должности и подразделения"
			)]
		ManageStructure = 15,

		/// <summary>
		/// Возможность редактировать награды
		/// </summary>
		[Permission(
			Name = "Редактирование наград",
			Description = "Разрешение создавать, удалять и редактировать награды"
			)]
		ManageRewards = 16,

		/// <summary>
		/// Разрешение менять чужие никнеймы
		/// </summary>
		[Permission(
			Name = "Изменять никнеймы",
			Description = "Разрешение изменять чужие никнеймы"
			)]
		ModerateNicknames = 17
	}
}