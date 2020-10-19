using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SKB.Base;
using SKB.Base.Synchronize;
using SKB.Base.Ref;
using DocsVision.BackOffice.ObjectModel;

namespace SKB.Archive
{
    /// <summary>
    /// Измение в строке таблицы "Документы".
    /// </summary>
    internal class DocumentsTableChange : MyRowChange
    {
        /// <summary>
        /// Поле «Карточка документа».
        /// </summary>
        public ChangingValue<Guid> DocumentsCard { get; private set; }
        /// <summary>
        /// Поле «Тип».
        /// </summary>
        public ChangingValue<Boolean> IsApproved { get; private set; }
        /// <summary>
        /// Поле «Тип».
        /// </summary>
        public ChangingValue<DateTime> ApprovalDate { get; private set; }
        /// <summary>
        /// Строка изменена.
        /// </summary>
        public override Boolean IsChanged
        {
            get
            {
                return DocumentsCard.IsChanged || IsApproved.IsChanged || ApprovalDate.IsChanged;
            }
        }
        DocumentsTableChange(Guid RowId) : base(RowId) { }
        public static explicit operator DocumentsTableChange(BaseCardProperty Row)
        {
            DocumentsTableChange Change = new DocumentsTableChange(Guid.Empty);
            Change.DocumentsCard = new ChangingValue<Guid>(Row[RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid());
            Change.IsApproved = new ChangingValue<Boolean>((Boolean)Row[RefAgreementOfDocumentsCard.Documents.IsApproved]);
            Change.ApprovalDate = new ChangingValue<DateTime>((DateTime)Row[RefAgreementOfDocumentsCard.Documents.ApprovalDate]);
            return Change;
        }
    }
}
