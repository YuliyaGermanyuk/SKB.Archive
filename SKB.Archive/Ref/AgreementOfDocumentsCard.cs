using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SKB.Base.Ref
{
    /// <summary>
    /// Схема карточки "Согласование документации".
    /// </summary>
    public static class RefAgreementOfDocumentsCard
    {
        /// <summary>
        /// Псевдоним карточки.
        /// </summary>
        public const String Alias = "AgreementOfDocumentsCard";
        /// <summary>
        /// Название карточки.
        /// </summary>
        public const String Name = "Согласование документации";
        /// <summary>
        /// Идентификатор типа карточки.
        /// </summary>
        public static readonly Guid ID = new Guid("{E33CD7E8-58AE-4444-8380-92D978683078}");
        /// <summary>
        /// Название правила для получения номера.
        /// </summary>
        public const String NumberRuleName = "СКБ Согласование документации";
        /// <summary>
        /// Команды ленты.
        /// </summary>
        public static class RibbonItems
        {
            /// <summary>
            /// Команда "Отправить".
            /// </summary>
            public const String Send = "Send";
            /// <summary>
            /// Команда "Делегировать".
            /// </summary>
            public const String Delegate = "Delegate";
            /// <summary>
            /// Команда "Утвердить".
            /// </summary>
            public const String Approve = "Approve";
            /// <summary>
            /// Команда "Утвердить только выделенные документы".
            /// </summary>
            public const String ApproveChecked = "ApproveChecked";
            /// <summary>
            /// Команда "Отклонить".
            /// </summary>
            public const String Reject = "Reject";
            /// <summary>
            /// Команда "Вернуть на доработку".
            /// </summary>
            public const String Return = "Return";
            /// <summary>
            /// Команда "Отозвать".
            /// </summary>
            public const String Revoke = "Revoke";
            /// <summary>
            /// Команда "Ознакомлен".
            /// </summary>
            public const String Close = "Close";
        }
        /// <summary>
        /// Роли карточки.
        /// </summary>
        public static class UserRoles
        {
            /// <summary>
            /// Администратор.
            /// </summary>
            public const String Admin = "Admin";
            /// <summary>
            /// Все.
            /// </summary>
            public const String AllUsers = "AllUsers";
            /// <summary>
            /// Команда "Регистратор".
            /// </summary>
            public const String Creator = "Creator";
            /// <summary>
            /// Текущий исполнитель
            /// </summary>
            public const String Performer = "Performer";
        }
        /// <summary>
        /// Состояние карточки.
        /// </summary>
        public class CardState
        {
            /// <summary>
            /// Не начата.
            /// </summary>
            public const String NotStarted = "NotStarted";
            /// <summary>
            /// На простом согласовании.
            /// </summary>
            public const String InSimpleAgreement = "InSimpleAgreement";
            /// <summary>
            /// На сложном согласовании.
            /// </summary>
            public const String InSmartAgreement = "InSmartAgreement";
            /// <summary>
            /// На доработке.
            /// </summary>
            public const String InReworking = "InReworking";
            /// <summary>
            /// Завершено.
            /// </summary>
            public const String Completed = "Completed";
            /// <summary>
            /// Закрыто.
            /// </summary>
            public const String Closed = "Closed";
        }
        /// <summary>
        /// Основная секция карточки.
        /// </summary>
        public static class MainInfo
        {
            /// <summary>
            /// Отображаемое состояние карточки.
            /// </summary>
            public enum DisplayCardState
            {
                /// <summary>
                /// Не начата.
                /// </summary>
                [Description("NotStarted")]
                NotStarted = 0,
                /// <summary>
                /// На согласовании.
                /// </summary>
                [Description("InAgreement")]
                InAgreement = 1,
                /// <summary>
                /// На доработке.
                /// </summary>
                [Description("InReworking")]
                InReworking = 2,
                /// <summary>
                /// Утверждено.
                /// </summary>
                [Description("Approved")]
                Approved = 3,
                /// <summary>
                /// Утверждено частично.
                /// </summary>
                [Description("PartiallyApproved")]
                PartiallyApproved = 4,
                /// <summary>
                /// Отклонено.
                /// </summary>
                [Description("Rejected")]
                Rejected = 5,
            };
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "MainInfo";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{7BC0BCC4-9639-4C4E-BA0A-7732ECC0B16F}");
            /// <summary>
            /// Поле "Номер".
            /// </summary>
            public const String Number = "Number";
            /// <summary>
            /// Поле "Дата регистрации".
            /// </summary>
            public const String CreationDate = "CreationDate";
            /// <summary>
            /// Поле "Регистратор".
            /// </summary>
            public const String Registrar = "Registrar";
            /// <summary>
            /// Поле "Дата отправки".
            /// </summary>
            public const String SentDate = "SentDate";
            /// <summary>
            /// Поле "Исполнитель".
            /// </summary>
            public const String Performer = "Performer";
            /// <summary>
            /// Поле "Дата завершения".
            /// </summary>
            public const String EndDate = "EndDate";
            /// <summary>
            /// Поле "Состояние".
            /// </summary>
            public const String State = "State";
            /// <summary>
            /// Поле "Комментарий регистратора".
            /// </summary>
            public const String CreatorsComment = "CreatorsComment";
            /// <summary>
            /// Поле "Частичное утверждение".
            /// </summary>
            public const String PartiallyApproval = "PartiallyApproval";
            /// <summary>
            /// Поле "Комментарий исполнителя".
            /// </summary>
            public const String PerformersComment = "PerformersComment";
        }
        /// <summary>
        /// Секция "Приборы".
        /// </summary>
        public static class Devices
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "Devices";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{C75A903A-8C1F-4FC6-BFAC-3D98B614692E}");
            /// <summary>
            /// Поле "Идентификатор прибора".
            /// </summary>
            public const String Id = "Id";
        }
        /// <summary>
        /// Секция "Документы".
        /// </summary>
        public static class Documents
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "Documents";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{C84DA9B9-0F1C-47ED-9C97-F97B1C74AE95}");
            /// <summary>
            /// Поле "Идентификатор кода СКБ".
            /// </summary>
            public const String CodeID = "CodeID";
            /// <summary>
            /// Поле "Название документа".
            /// </summary>
            public const String DocumentsName = "DocumentsName";
            /// <summary>
            /// Поле "Карточка документа".
            /// </summary>
            public const String DocumentsCard = "DocumentsCard";
            /// <summary>
            /// Поле "Идентификатор категории".
            /// </summary>
            public const String DocumentsCategory = "DocumentsCategory";
            /// <summary>
            /// Поле "Версия документа".
            /// </summary>
            public const String DocumentsVersion = "DocumentsVersion";
            /// <summary>
            /// Поле "Автор документа".
            /// </summary>
            public const String DocumentsAuthor = "DocumentsAuthor";
            /// <summary>
            /// Поле "Комментарий".
            /// </summary>
            public const String DocumentsComment = "DocumentsComment";
            /// <summary>
            /// Поле "Утвержден".
            /// </summary>
            public const String IsApproved = "IsApproved";
            /// <summary>
            /// Поле "Дата утверждения".
            /// </summary>
            public const String ApprovalDate = "ApprovalDate";
            /// <summary>
            /// Поле "Идентификатор".
            /// </summary>
            public const String Id = "Id";
            /// <summary>
            /// Поле "Идентификатор связанной строки".
            /// </summary>
            public const String RefRowId = "RefRowId";
        }
        /// <summary>
        /// Секция "Ход согласования".
        /// </summary>
        public static class AgreementHistory
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "AgreementHistory";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{15A549C7-4E6F-4F65-90D6-331EAF1756FC}");
            /// <summary>
            /// Поле "Дата события".
            /// </summary>
            public const String EventDate = "EventDate";
            /// <summary>
            /// Поле "Инициатор события".
            /// </summary>
            public const String EventCreator = "EventCreator";
            /// <summary>
            /// Поле "Описание события".
            /// </summary>
            public const String EventDescription = "EventDescription";
        }
        /// <summary>
        /// Операции карточки.
        /// </summary>
        public static class Operations
        {
            /// <summary>
            /// Изменение документов.
            /// </summary>
            public const String ChangeDocuments = "ChangeDocuments";
        }
    }
}