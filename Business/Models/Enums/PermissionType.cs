namespace Business.Models.Enums
{
    public enum PermissionType
    {
		/// <summary>
		/// Все разрешения в одном и
		/// обход ограничений по работе 
		/// с вышестоящими должностями
		/// </summary>
        Administrator = 1,

		/// <summary>
		/// Боец не получает автоматические 
		/// выговора и благодарности
		/// за обязательные сборы
		/// </summary>
		ConstantBeggOf = 2,

		/// <summary>
		/// Разрешение регистрировать новых бойцов
		/// </summary>
		RegisterNewUnits = 3,

		/// <summary>
		/// Разрешение уволнять бойцов
		/// </summary>
		DismissUnits = 4,

		/// <summary>
		/// Разрешение отправлять бойцов в отставку
		/// </summary>
		AssignRetirement = 5,

		/// <summary>
		/// Разрешение на подтверждение своей и чужой активности
		/// </summary>
		FixActivity = 6,

		/// <summary>
		/// Разрешение на выход в отпуск
		/// </summary>
		VacationAccess = 7,

		/// <summary>
		/// Разрешение на выход в отставку
		/// </summary>
		AccessRetirement = 8,

		/// <summary>
		/// Возможность выдавать благодарности и выговора
		/// </summary>
		AssignStatuses = 9,

		/// <summary>
		/// Возможность отправлять бойцов в отпуск
		/// </summary>
		ForceVacation = 10,

		/// <summary>
		/// Возможность присваивать звания бойцам
		/// </summary>
		AssignRanks = 11,

		/// <summary>
		/// Возможность менять должности нижестоящих бойцов
		/// </summary>
		AssignPosts = 12,

		/// <summary>
		/// Возможность награждать бойцов
		/// </summary>
		AssignRewards = 13,

		/// <summary>
		/// Возможность редактировать звания
		/// </summary>
		ManageRanks = 14,

		/// <summary>
		/// Возможность редактировать 
		/// нижестоящие должности и подразделения
		/// </summary>
		ManageStructure = 15,

		/// <summary>
		/// Возможность редактировать награды
		/// </summary>
		ManageRewards = 16,

		/// <summary>
		/// Разрешение менять чужие никнеймы
		/// </summary>
		ModerateNicknames = 17,

		/// <summary>
		/// Разрешение видеть скрытые документы
		/// </summary>
		SeeHiddenDocs = 18,

		/// <summary>
		/// Разрешение загружать новые документы
		/// </summary>
		UploadDocs = 19
	}
}