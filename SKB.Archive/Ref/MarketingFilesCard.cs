using DocsVision.Platform.ObjectManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKB.Base.Ref
{
    /// <summary>
    /// Схема карточки "Файлы".
    /// </summary>
    public static class RefMarketingFilesCard
    {
        /// <summary>
        /// Псевдоним карточки.
        /// </summary>
        public const String Alias = "MarketingFilesCard";
        /// <summary>
        /// Идентификатор типа карточки.
        /// </summary>
        public static readonly Guid ID = new Guid("{32166A90-620A-4ECE-BF78-21E61D205977}");
        /// <summary>
        /// Название карточки.
        /// </summary>
        public const String Name = "Файлы";
        /// <summary>
        /// Действия карточки.
        /// </summary>
        public static class Actions
        {
            /// <summary>
            /// Действие "Открыть файлы".
            /// </summary>
            public static readonly Guid OpenFiles = new Guid("{8412928A-A1C0-4064-B472-32C620EB5862}");
            /// <summary>
            /// Действие "Открыть карточку и файлы".
            /// </summary>
            public static readonly Guid OpenCardAndFiles = new Guid("{34AF6803-1682-48AE-9CC5-602301254645}");
            /// <summary>
            /// Действие "Открыть карточку".
            /// </summary>
            public static readonly Guid OpenCard = new Guid("{C37CCDD5-DCDE-47C1-B137-0590490637BC}");
            /// <summary>
            /// Действие "Удалить карточку и файлы".
            /// </summary>
            public static readonly Guid Delete = new Guid("{14786E3F-84A4-4805-8F4F-8A5B287ADB29}");
        }
        /// <summary>
        /// Режимы открытия карточки карточки.
        /// </summary>
        public static class Modes
        {
            /// <summary>
            /// Режим открытия "Открытие файлов".
            /// </summary>
            public static readonly Guid OpenFiles = new Guid("{80BE776F-B3FA-44AA-AC11-8F0B1B639BAE}");
            /// <summary>
            /// Режим открытия "Открытие карточки и файлов".
            /// </summary>
            public static readonly Guid OpenCardAndFiles = new Guid("{F1A3B952-25D8-4583-9AF4-249857978E3C}");
            /// <summary>
            /// Режим открытия "Открытие карточки".
            /// </summary>
            public static readonly Guid OpenCard = new Guid("{9F859428-B45A-4420-B490-9E4050F1D79A}");
        }
        /// <summary>
        /// Кнопки карточки.
        /// </summary>
        public static class Buttons
        {
            /// <summary>
            /// Кнопка "Загрузить файлы".
            /// </summary>
            public const String Upload = "Upload";
        }
        /// <summary>
        /// Основная секция карточки.
        /// </summary>
        public static class MainInfo
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "MainInfo";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{212F1B22-1E1E-4027-87B8-FBE817F368CA}");
            /// <summary>
            /// Поле "Название документа".
            /// </summary>
            public const String Name = "Name";
            /// <summary>
            /// Поле "Дата создания".
            /// </summary>
            public const String CreationDate = "CreationDate";
            /// <summary>
            /// Поле "Регистратор".
            /// </summary>
            public const String Registrar = "Registrar";
            /// <summary>
            /// Поле "Файлы документа".
            /// </summary>
            public const String Files = "Files";
            /// <summary>
            /// Поле "Папка".
            /// </summary>
            public const String Folder = "Folder";
            /// <summary>
            /// Поле "Идентификатор контрагента".
            /// </summary>
            public const String PartnerId = "PartnerId";
            /// <summary>
            /// Поле "Состояние".
            /// </summary>
            public const String State = "State";
            /// <summary>
            /// Поле "Идентификатор номера".
            /// </summary>
            public const String NumberId = "NumberId";
            /// <summary>
            /// Поле "Мероприятие".
            /// </summary>
            public const String Event = "Event";
        }
        /// <summary>
        /// Секция "Категории".
        /// </summary>
        public static class Categories
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "Categories";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{F50F2FB6-3191-437A-9876-6F0214C1ECC8}");
            /// <summary>
            /// Поле "Категория".
            /// </summary>
            public const String CategoryId = "CategoryId";
            /// <summary>
            /// Ссылочное поле "Категория".
            /// </summary>
            public const String Category = "Category";
        }
        /// <summary>
        /// Секция "Свойства".
        /// </summary>
        public static class Properties
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "Properties";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{31B009D2-BA1A-421B-8915-4D6FF3E2B5FC}");
            /// <summary>
            /// Поле "Название свойства".
            /// </summary>
            public const String Name = "Name";
            /// <summary>
            /// Поле "Значение свойства".
            /// </summary>
            public const String Value = "Value";
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
            public static readonly Guid ID = new Guid("{4589B667-E9BB-4CB1-A131-E2ABD6E0E486}");
            /// <summary>
            /// Поле "Идентификатор прибора".
            /// </summary>
            public const String Id = "Id";
        }
        /// <summary>
        /// Секция "Виды оборудования".
        /// </summary>
        public static class EquipmentSorts
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "EquipmentSorts";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{9BC85CE6-02B9-4D21-84EF-E1BE642264D1}");
            /// <summary>
            /// Поле "Вид оборудования".
            /// </summary>
            public const String Sort = "Sort";
        }
        /// <summary>
        /// Секция "Типы оборудования".
        /// </summary>
        public static class EquipmentTypes
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "EquipmentTypes";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{D53F394C-B15C-422E-B070-A1D7949CE91B}");
            /// <summary>
            /// Поле "Тип оборудования".
            /// </summary>
            public const String Type = "Type";
        }
        /// <summary>
        /// Секция "Производители".
        /// </summary>
        public static class Manufacturers
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "Manufacturers";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{5656543C-5104-4AE4-B52C-9AFE6353A97D}");
            /// <summary>
            /// Поле "Производитель".
            /// </summary>
            public const String Manufacturer = "Manufacturer";
        }
        /// <summary>
        /// Секция "Марки".
        /// </summary>
        public static class Makes
        {
            /// <summary>
            /// Псевдоним секции.
            /// </summary>
            public const String Alias = "Makes";
            /// <summary>
            /// Идентификатор секции.
            /// </summary>
            public static readonly Guid ID = new Guid("{89561E94-5DCE-41CB-8E78-7C77DEAFCFF3}");
            /// <summary>
            /// Поле "Марка".
            /// </summary>
            public const String Make = "Make";
        }
    }
}