using DocsVision.Platform.ObjectManager;
using SKB.Base.Enums;
using SKB.Base.Ref;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SKB.Base.AssignRights
{
    /// <summary>
    /// Надстройка для карточки Файлы Маркетинга.
    /// </summary>
    public class ExtraCardMarketingFiles : ExtraCard
    {
        /// <summary>
        /// Строка секция "Основная информация" карточки Файлы Маркетинга.
        /// </summary>
        RowData MainInfoRow;
        /// <summary>
        /// Инициализирует надстройку для карточки.
        /// </summary>
        /// <param name="Card">Данные карточки.</param>
        ExtraCardMarketingFiles (CardData Card)
            : base(Card)
        {
            MainInfoRow = Card.Sections[RefMarketingFilesCard.MainInfo.ID].FirstRow;
        }
        /// <summary>
        /// Возвращает надстройку для карточки Файлы Маркетинга.
        /// </summary>
        /// <param name="Card">Данные карточки.</param>
        public static ExtraCardMarketingFiles GetExtraCard (CardData Card)
        {
            return !Card.IsNull() && Card.Type.Id == RefMarketingFilesCard.ID ? new ExtraCardMarketingFiles(Card) : null;
        }
        /// <summary>
        /// Тип карточки.
        /// </summary>
        public override String CardType
        {
            get
            {
                return "Файлы Маркетинга";
            }
        }
        /// <summary>
        /// Название - Название документа.
        /// </summary>
        public override String Name
        {
            get
            {
                return MainInfoRow.GetString(RefMarketingFilesCard.MainInfo.Name) ?? String.Empty;
            }
            set
            {
                MainInfoRow.SetString(RefMarketingFilesCard.MainInfo.Name, value);
            }
        }
        /// <summary>
        /// Полное обозначение категории/типа - Категории(Тип документа).
        /// </summary>
        public override String Category
        {
            get
            {
                return Card.Sections[RefMarketingFilesCard.Categories.ID].Rows.Select(row => row.GetString(RefMarketingFilesCard.Categories.Category)).Where(s => !s.IsNull()).Aggregate("; ");
            }
        }
        /// <summary>
        /// Идентификатор категории из справочника.
        /// </summary>
        public override Guid CategoryId
        {
            get
            {
                RowData CategoryRow = Card.Sections[RefMarketingFilesCard.Categories.ID].Rows.FirstOrDefault();
                return CategoryRow.IsNull() ? Guid.Empty : CategoryRow.GetGuid(RefMarketingFilesCard.Categories.CategoryId) ?? Guid.Empty;
            }
        }
        /// <summary>
        /// Название категории из справочника.
        /// </summary>
        public override String CategoryName
        {
            get
            {
                RowData CategoryRow = Card.Sections[RefMarketingFilesCard.Categories.ID].Rows.FirstOrDefault();
                return CategoryRow.IsNull() ? String.Empty : CategoryRow.GetString(RefMarketingFilesCard.Categories.Category) ?? String.Empty;
            }
        }
        /// <summary>
        /// Путь к файлу - Папка.
        /// </summary>
        public override String Path
        {
            get
            {
                return MainInfoRow.GetString(RefMarketingFilesCard.MainInfo.Folder) ?? String.Empty;
            }
            set
            {
                MainInfoRow.SetString(RefMarketingFilesCard.MainInfo.Folder, value);
            }
        }
        /// <summary>
        /// Возвращает строковое представление ExtraCardMarketingFiles.
        /// </summary>
        /// <returns></returns>
        public override String ToString ()
        {
            return String.Format("{1}. {0}", Name, Category);
        }
    }
}