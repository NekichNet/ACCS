using Business.Models.Util;

namespace Business.Models.Enums
{
    public enum PermissionType
    {
		/// обход ограничений по работе 
		/// с вышестоящими должностями
		/// </summary>
		[PermissionAttribute(
			Name = "Администратор",
			Description = "Все разрешения в одном и обход ограничений по работе с вышестоящими должностями"
			)]
		Administrator = 1,

		/// <summary>
		/// Боец не получает автоматические 
		/// выговора и благодарности
		/// за обязательные сборы
		/// </summary>
		[PermissionAttribute(
			Name = "Освобождение от сборов",
			Description = "Боец не получает автоматические выговора и благодарности за обязательные сборы"
			)]
		ConstantBeggOf = 2,

		/// <summary>
		/// Разрешение регистрировать новых бойцов
		/// </summary>
		[PermissionAttribute(
			Name = "Регистрация новых бойцов",
			Description = "Разрешение принимать новичков в клан"
			)]
		RegisterNewUnits = 3,

		/// <summary>
		/// Разрешение уволнять бойцов
		/// </summary>
		[PermissionAttribute(
			Name = "Увольнение бойцов",
			Description = "Разрешение увольнять бойцов клана"
			)]
		DismissUnits = 4,

		/// <summary>
		/// Разрешение отправлять бойцов в отставку
		/// </summary>
		[PermissionAttribute(
			Name = "Отправление отставку",
			Description = "Разрешение отправлять бойцов клана в отставку"
			)]
		AssignRetirement = 5,

		/// <summary>
		/// Разрешение на подтверждение своей и чужой активности
		/// </summary>
		[PermissionAttribute(
			Name = "Подтверждение активности",
			Description = "Разрешение подтверждать фиксации своей и чужой активности"
			)]
		FixActivity = 6,

		/// <summary>
		/// Разрешение на выход в отпуск
		/// </summary>
		[PermissionAttribute(
			Name = "Выход в отпуск",
			Description = "Разрешение на выход в отпуск"
			)]
		VacationAccess = 7,

		/// <summary>
		/// Разрешение на выход в отставку
		/// </summary>
		[PermissionAttribute(
			Name = "Выход в отставку",
			Description = "Разрешение на выход в отставку"
			)]
		AccessRetirement = 8,

		/// <summary>
		/// Возможность выдавать благодарности и выговора
		/// </summary>
		[PermissionAttribute(
			Name = "Выдача статусов",
			Description = "Разрешение выдавать бойцам выговора, благодарности и строгие выговоры"
			)]
		AssignStatuses = 9,

		/// <summary>
		/// Возможность отправлять бойцов в отпуск
		/// </summary>
		[PermissionAttribute(
			Name = "Отправка в отпуск",
			Description = "Разрешение отправлять бойцов в отпуск"
			)]
		ForceVacation = 10,

		/// <summary>
		/// Возможность присваивать звания бойцам
		/// </summary>
		[PermissionAttribute(
			Name = "Присваивание званий",
			Description = "Разрешение изменять звания у бойцов"
			)]
		AssignRanks = 11,

		/// <summary>
		/// Возможность менять должности нижестоящих бойцов
		/// </summary>
		[PermissionAttribute(
			Name = "Назначение на должности",
			Description = "Разрешение назначать нижестоящих бойцов на должности"
			)]
		AssignPosts = 12,

		/// <summary>
		/// Возможность награждать бойцов
		/// </summary>
		[PermissionAttribute(
			Name = "Награждение бойцов",
			Description = "Разрешение награждать бойцов"
			)]
		AssignRewards = 13,

		/// <summary>
		/// Возможность редактировать звания
		/// </summary>
		[PermissionAttribute(
			Name = "Редактирование званий",
			Description = "Разрешение создавать, удалять и редактировать звания"
			)]
		ManageRanks = 14,

		/// <summary>
		/// Возможность редактировать 
		/// нижестоящие должности и подразделения
		/// </summary>
		[PermissionAttribute(
			Name = "Редактирование структуры",
			Description = "Разрешение создавать, удалять и редактировать нижестоящие должности и подразделения"
			)]
		ManageStructure = 15,

		/// <summary>
		/// Возможность редактировать награды
		/// </summary>
		[PermissionAttribute(
			Name = "Редактирование наград",
			Description = "Разрешение создавать, удалять и редактировать награды"
			)]
		ManageRewards = 16,

		/// <summary>
		/// Разрешение менять чужие никнеймы
		/// </summary>
		[PermissionAttribute(
			Name = "Изменять никнеймы",
			Description = "Разрешение изменять чужие никнеймы"
			)]
		ModerateNicknames = 17,

		/// <summary>
		/// Разрешение видеть скрытые документы
		/// </summary>
		[PermissionAttribute(
			Name = "Видеть скрытые документы",
			Description = "Разрешение видеть скрытые документы"
			)]
		SeeHiddenDocs = 18,

		/// <summary>
		/// Разрешение загружать новые документы
		/// </summary>
		[PermissionAttribute(
			Name = "Загружать документы",
			Description = "Разрешение загружать новые документы"
			)]
		UploadDocs = 19
	}
}