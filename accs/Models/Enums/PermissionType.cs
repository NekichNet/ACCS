namespace accs.Models.Enums
{
    public enum PermissionType
    {
        /// <summary>
        /// Все разрешения в одном и
        /// обход ограничений по работе 
        /// с вышестоящими должностями
        /// </summary>
        Administrator,

        /// <summary>
        /// Боец не получает  автоматические 
        /// выговора и благодарности
        /// за обязательные сборы
        /// </summary>
		AutoReprimandImmune,

        /// <summary>
        /// Разрешение регистрировать новых бойцов
        /// </summary>
        RegisterNewUnits,

        /// <summary>
        /// Разрешение уволнять бойцов
        /// </summary>
        DismissUnits,

        /// <summary>
        /// Разрешение отправлять бойцов в отставку
        /// </summary>
        AssignRetirement,

        /// <summary>
        /// Разрешение на подтверждение своей и чужой активности
        /// </summary>
		ConfirmActivity,

        /// <summary>
        /// Разрешение на выход в отпуск
        /// </summary>
        VacationAccess,

        /// <summary>
        /// Разрешение на выход в отставку
        /// </summary>
        AccessRetirement,

        /// <summary>
        /// Возможность выдавать благодарности и выговора
        /// </summary>
        GiveReprimandGratitude,

        /// <summary>
        /// Возможность отправлять бойцов в отпуск
        /// </summary>
        ForceVacation,

        /// <summary>
        /// Возможность присваивать звания бойцам
        /// </summary>
        AssignRanks,

        /// <summary>
        /// Возможность менять должности нижестоящих бойцов
        /// </summary>
        AssignPosts,

        /// <summary>
        /// Возможность награждать бойцов
        /// </summary>
		AssignRewards,

        /// <summary>
        /// Возможность редактировать звания
        /// </summary>
        ManageRanks,

        /// <summary>
        /// Возможность редактировать 
        /// нижестоящие должности и подразделения
        /// </summary>
		ManageStructure,

        /// <summary>
        /// Возможность редактировать награды
        /// </summary>
        ManageRewards,

        /// <summary>
        /// Разрешение менять чужие никнеймы
        /// </summary>
        ModerateNicknames
    }
}
