using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

using DocsVision.BackOffice.ObjectModel;
using DocsVision.BackOffice.WinForms.Design.LayoutItems;
using DocsVision.BackOffice.ObjectModel.Services;
using DocsVision.BackOffice.WinForms;

using DocsVision.TakeOffice.Cards.Constants;

using DocsVision.Platform.WinForms;
using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectManager.SearchModel;
using DocsVision.Platform.ObjectManager.Metadata;
using DocsVision.Platform.Security.AccessControl;

using DevExpress.Utils;
using DevExpress.XtraBars;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;

using RKIT.MyCollectionControl.Design.Control;
using RKIT.MyMessageBox;

using SKB.Base;
using SKB.Base.Ref;
using SKB.Base.Ref.Properties;
using SKB.Base.Enums;
using SKB.Base.AssignRights;
using SKB.Base.Task.Delegate;

namespace SKB.Archive.Cards
{
    /// <summary>
    /// Карточка "Согласование документации"
    /// </summary>
    public class AgreementOfDocumentsCard : MyBaseCard
    {
        #region Properties
        /// <summary>
        /// Коллекционное поле "Приборы".
        /// </summary>
        CollectionControlView Devices_Collection;
        /// <summary>
        /// Таблица "Документы".
        /// </summary>
        ITableControl Table_Documents;
        /// <summary>
        /// Таблица "Ход согласования".
        /// </summary>
        ITableControl Table_History;
        /// <summary>
        /// Таблица "Документы".
        /// </summary>
        GridView Grid_Documents;
        /// <summary>
        /// Таблица "Ход согласования".
        /// </summary>
        GridView Grid_History;
        /// <summary>
        /// Список табличных изменений.
        /// </summary>
        List<DocumentsTableChange> Doc_Changes;
        /// <summary>
        /// Текущий пользователь
        /// </summary>
        StaffEmployee CurrentEmployee;
        /// <summary>
        /// Роль текущего пользователя
        /// </summary>
        String CurrentUserRole
        {
            get
            {
                Guid CurrentEmployeeID = Context.GetObjectRef<StaffEmployee>(CurrentEmployee).Id;

                // Администратор
                if (Context.GetService<IStaffService>().FindEmployeeGroups(CurrentEmployee).Any(r => r.Name == "DocsVision Administrators"))
                    return RefAgreementOfDocumentsCard.UserRoles.Admin;
                // Регистратор
                if (CurrentEmployeeID.Equals(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Registrar).ToGuid()))
                    return RefAgreementOfDocumentsCard.UserRoles.Creator;
                // Текущий исполнитель
                if (CurrentEmployeeID.Equals(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Performer).ToGuid()))
                    return RefAgreementOfDocumentsCard.UserRoles.Performer;
                // Прочие сотрудники
                return RefAgreementOfDocumentsCard.UserRoles.AllUsers;
            }
        }
        /// <summary>
        /// Поле "Состояние"
        /// </summary>
        ComboBoxEdit Edit_State;
        /// <summary>
        /// Поле "Частичное утверждение"
        /// </summary>
        CheckEdit Edit_PartiallyApproval;
        /// <summary>
        /// Возможно частичное утверждение
        /// </summary>
        bool PartiallyApproval;
        /// <summary>
        /// Удаленные документы.
        /// </summary>
        List<Guid> RemovedDocuments;
        /// <summary>
        /// Удаленные документы.
        /// </summary>
        List<Guid> SynchronizeRows;
        /// <summary>
        /// Предвыбранная папка для поиска документов - Поиск конструкторских документов.
        /// </summary>
        public readonly static Guid RefPreSelectedFolderForDocuments = new Guid("{361E4B91-BF31-434E-A490-B6DF6DD4453E}");
        #endregion

        /// <summary>
        /// Инициализирует карточку по заданным данным.
        /// </summary>
        /// <param name="ClassBase">Скрипт карточки.</param>
        public AgreementOfDocumentsCard(ScriptClassBase ClassBase) : base(ClassBase)
        {
            try
            {
                // Получение рабочих объектов
                Devices_Collection = ICardControl.FindPropertyItem<CollectionControlView>(RefAgreementOfDocumentsCard.Devices.Alias);
                Table_Documents = ICardControl.FindPropertyItem<ITableControl>(RefAgreementOfDocumentsCard.Documents.Alias);
                Table_History = ICardControl.FindPropertyItem<ITableControl>(RefAgreementOfDocumentsCard.AgreementHistory.Alias);
                Edit_State = ICardControl.FindPropertyItem<ComboBoxEdit>(RefAgreementOfDocumentsCard.MainInfo.State);
                Edit_PartiallyApproval = ICardControl.FindPropertyItem<CheckEdit>(RefAgreementOfDocumentsCard.MainInfo.PartiallyApproval);

                Grid_Documents = ICardControl.GetGridView(RefAgreementOfDocumentsCard.Documents.Alias);
                Grid_History = ICardControl.GetGridView(RefAgreementOfDocumentsCard.AgreementHistory.Alias);

                IStaffService StaffService = Context.GetService<IStaffService>();
                CurrentEmployee = StaffService.GetCurrentEmployee();
                PartiallyApproval = (bool?)GetControlValue(RefAgreementOfDocumentsCard.MainInfo.PartiallyApproval) ?? false;

                // Получение номера
                if (GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Number).ToGuid().IsEmpty())
                {
                    CurrentNumerator = CardScript.GetNumber(RefAgreementOfDocumentsCard.NumberRuleName);
                    CurrentNumerator.Number = Convert.ToInt32(CurrentNumerator.Number).ToString("00000");
                    SetControlValue(RefAgreementOfDocumentsCard.MainInfo.Number, Context.GetObjectRef<BaseCardNumber>(CurrentNumerator).Id);
                    WriteLog("Выдали номер: " + CurrentNumerator.Number);
                }
                else
                {
                    CurrentNumerator = Context.GetObject<BaseCardNumber>(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Number));
                }

                /* Cписок удаленных документов */
                RemovedDocuments = new List<Guid>();

                /* Cписок документов, требующих синхронизации */
                SynchronizeRows = new List<Guid>();

                /* Привязка методов */
                if (!IsReadOnly)
                {
                    CardScript.CardControl.CardClosed += CardControl_CardClosed;
                    CardScript.CardControl.Saved += CardControl_Saved;
                    CardScript.CardControl.Saving += CardControl_Saving;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Send].ItemClick -= Send_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Send].ItemClick += Send_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Delegate].ItemClick -= Delegate_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Delegate].ItemClick += Delegate_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Approve].ItemClick -= Approve_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Approve].ItemClick += Approve_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.ApproveChecked].ItemClick -= ApproveChecked_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.ApproveChecked].ItemClick += ApproveChecked_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Return].ItemClick -= Return_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Return].ItemClick += Return_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Reject].ItemClick -= Reject_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Reject].ItemClick += Reject_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Revoke].ItemClick -= Revoke_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Revoke].ItemClick += Revoke_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Close].ItemClick -= Close_ItemClick;
                    ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Close].ItemClick += Close_ItemClick;

                    ICardControl.RemoveTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 6);
                    ICardControl.RemoveTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 5);
                    ICardControl.RemoveTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 4);
                    ICardControl.RemoveTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 3);
                    ICardControl.AddTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, "Добавить", MyHelper.Image_Table_Add).ItemClick += AddDocumentButton_ItemClick;
                    ICardControl.AddTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, "Удалить", MyHelper.Image_Table_Remove).ItemClick += RemoveDocumentButton_ItemClick;
                    ICardControl.AddTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, "Открыть карточку", MyHelper.Image_Table_OpenCard).ItemClick += OpenCardDocumentButton_ItemClick;
                    ICardControl.AddTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, "Открыть папку", MyHelper.Image_Table_OpenFolder).ItemClick += OpenFolderDocumentButton_ItemClick;

                    Edit_State.EditValueChanged -= State_EditValueChanged;
                    Edit_State.EditValueChanged += State_EditValueChanged;
                    Edit_PartiallyApproval.EditValueChanged -= PartiallyApproval_EditValueChanged;
                    Edit_PartiallyApproval.EditValueChanged += PartiallyApproval_EditValueChanged;
                }
                Grid_Documents.AddDoubleClickHandler(new Documents_DoubleClickDelegate(Documents_DoubleClick));
                
                //AddTableHandler(RefAgreementOfDocumentsCard.Documents.Alias, "AddButtonClicked", "Devices_AddButtonClicked");
                //AddTableHandler(RefAgreementOfDocumentsCard.Documents.Alias, "RemoveButtonClicked", "Devices_RemoveButtonClicked");
                
                // Настройка внешнего вида
                Customize();
            }
            
            catch (Exception Ex) { CallError(Ex); }
        }

        #region Delegates

        private delegate void Documents_DoubleClickDelegate(Object sender, EventArgs e);

        #endregion

        #region Methods
        /// <summary>
        /// Настройка внешнего вида.
        /// </summary>
        public override void Customize()
        {
            // Настройка таблиц

            // Таблица "Документы"
            foreach (DevExpress.XtraGrid.Columns.GridColumn iCol in Grid_Documents.Columns)
                iCol.AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
            
            Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.DocumentsVersion].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
            Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.ApprovalDate].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
            Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.CodeID].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
            Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.DocumentsName].AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.DocumentsComment].AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;

            if (Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.ApprovalDate].ColumnEdit is RepositoryItemDateEdit)
            {
                RepositoryItemDateEdit Repository = Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.ApprovalDate].ColumnEdit as RepositoryItemDateEdit;
                Repository.Mask.EditMask = "dd.MM.yyyy";
                Repository.Mask.UseMaskAsDisplayFormat = true;
            }

            string CurrentState = CardScript.BaseObject.SystemInfo.State.DefaultName;
            switch (CurrentState)
            {
                case RefAgreementOfDocumentsCard.CardState.InSmartAgreement:
                    Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.IsApproved].Visible = true;
                    Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.IsApproved].ColumnEdit.ReadOnly = false;
                    Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.ApprovalDate].Visible = true;
                    break;
                case RefAgreementOfDocumentsCard.CardState.Completed:
                case RefAgreementOfDocumentsCard.CardState.Closed:
                    Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.IsApproved].Visible = true;
                    Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.IsApproved].ColumnEdit.ReadOnly = true;
                    Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.ApprovalDate].Visible = true;
                    break;
                default:
                    Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.IsApproved].Visible = false;
                    Grid_Documents.Columns[RefAgreementOfDocumentsCard.Documents.ApprovalDate].Visible = false;
                    break;
            }

            ICardControl.HideTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 0, true);
            ICardControl.HideTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 1, true);
            ICardControl.HideTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 2, true);

            if (Table_Documents.RowCount == 0)
                ICardControl.DisableTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 4, true);
            if (!Context.IsOperationAllowed(CardScript.BaseObject, RefAgreementOfDocumentsCard.Operations.ChangeDocuments))
            {
                ICardControl.DisableTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 3, true);
                ICardControl.DisableTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 4, true);
            }


            // Таблица "Ход согласования"
            foreach (DevExpress.XtraGrid.Columns.GridColumn iCol in Grid_History.Columns)
                iCol.AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;

            Grid_History.Columns[RefAgreementOfDocumentsCard.AgreementHistory.EventDate].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
            //Grid_Documents.Columns[RefAgreementOfDocumentsCard.AgreementHistory.EventDescription].AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;

            // Настройка кнопок
            ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Send].Hint = "Отправить документы на согласование.";
            ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Delegate].Hint = "Делегировать согласование документов другому сотруднику.";
            ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Approve].Hint += "Утвердить все документы.";
            ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.ApproveChecked].Hint = "Утвердить только отмеченные документы.";
            ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Return].Hint = "Вернуть документы на доработку.";
            ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Reject].Hint += "Отклонить все документы.";
            ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Revoke].Hint += "Откатить процесс в предыдущее состояние.";
            ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Close].Hint += "С результатами согласования ознакомлен.";
        }
        /// <summary>
        /// Обновляет список изменений.
        /// </summary>
        public override void RefreshChanges()
        {
            if (Doc_Changes.IsNull())
                Doc_Changes = new List<DocumentsTableChange>();
            else
                Doc_Changes.Clear();

            for (Int32 i = 0; i < Table_Documents.RowCount; i++)
                Doc_Changes.Add((DocumentsTableChange)Table_Documents[i]);
        }
        
        /// <summary>
        /// Добавление записи в таблицу "Ход согласования"
        /// </summary>
        /// <param name="EventDate"> Дата события.</param>
        /// <param name="EventCreator">Инициатор события.</param>
        /// <param name="EventDescription">Описание события.</param>
        private void NewHistoryRow(DateTime EventDate, StaffEmployee EventCreator, String EventDescription)
        {
            BaseCardProperty NewHistoryRow = Table_History.AddRow(CardScript.BaseObject);
            NewHistoryRow[RefAgreementOfDocumentsCard.AgreementHistory.EventDate] = EventDate;
            NewHistoryRow[RefAgreementOfDocumentsCard.AgreementHistory.EventCreator] = Context.GetObjectRef(EventCreator).Id;
            NewHistoryRow[RefAgreementOfDocumentsCard.AgreementHistory.EventDescription] = EventDescription;
            Table_History.RefreshRows();
        }
        /// <summary>
        /// Добавление новой записи в таблицу "Документы"
        /// </summary>
        /// <param name="DocumentsCardData"> Карточка документа.</param>
        private bool AddDocument(CardData DocumentsCardData)
        {
            try
            {
                BaseCardProperty NewRow = Table_Documents.AddRow(CardScript.BaseObject);
                if (!RefreshDocument(NewRow, DocumentsCardData))
                    Table_Documents.RemoveRow(CardScript.BaseObject, NewRow);
                return true;
            }
            catch (Exception Ex) 
            { 
                MyMessageBox.Show(Ex.Message);
                return false;
            }
        }
        /// <summary>
        /// Удаление записи из таблицы "Документы"
        /// </summary>
        /// <param name="DocumentsRow"> Удаляемая строка таблицы.</param>
        private bool RemoveDocument(BaseCardProperty DocumentsRow)
        {
            try 
            {
                if ((bool)DocumentsRow[RefAgreementOfDocumentsCard.Documents.IsApproved])
                    throw new MyException("Вы не можете удалить утвержденный документ!");
                Table_Documents.RemoveRow(CardScript.BaseObject, DocumentsRow);
                return true;
            }
            catch (Exception Ex) 
            {
                MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false; 
            }
        }
        /// <summary>
        /// Добавление новых приборов в перечень
        /// </summary>
        /// <param name="DevicesList"> Перечень приборов нового документа.</param>
        private bool AddDevices(List<String> DevicesList)
        {
            try
            {
                IEnumerable<String> NewDevices = DevicesList.Except(Devices_Collection.SelectedItems.Select(r => r.ObjectId.ToString().ToUpper()));
                if (!NewDevices.IsNull())
                {
                    foreach (String DeviceId in NewDevices)
                    {
                        Devices_Collection.AddUniversalItem(DeviceId.ToGuid());
                        /*MyMultiChooseBoxItem NewItem = new MyMultiChooseBoxItem();
                        NewItem.DisplayValue = DeviceId.ToGuid();
                        NewItem.DisplayValue = UniversalCard.GetItemName(DeviceId.ToGuid());
                        Devices_Collection.AddItem(NewItem);*/
                    }
                }
                return true;
            }
            catch (Exception Ex)
            {
                MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        /// <summary>
        /// Удаление лишних приборов из перечня
        /// </summary>
        /// <param name="DevicesList"> Перечень приборов актуальный.</param>
        private bool RemoveDevices(List<String> DevicesList)
        {
            try
            {
                List<Guid> AllDevices = Devices_Collection.SelectedItems.Select(r => r.ObjectId).ToList();
                List<Guid> DeletedDevices = AllDevices.Except(DevicesList.Select(r => r.ToGuid())).ToList();
                if (!DeletedDevices.IsNull())
                {
                    //List<Guid> NewDevicesId = new List<Guid>();
                    //IEnumerable<MyMultiChooseBoxItem> NewDevicesItems = Devices_Collection.SelectedItems.Where(r => NewDevices.Any(s => s == r.ObjectId.ToString().ToUpper()));
                    //foreach (MyMultiChooseBoxItem Item in NewDevicesItems)
                    //    NewDevicesId.Add(Item.ObjectId);
                    foreach (Guid DeviceItem in DeletedDevices)
                        Devices_Collection.RemoveItem(DeviceItem);
                }
                return true;
            }
            catch (Exception Ex)
            {
                MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        /// <summary>
        /// Обновление перечня приборов
        /// </summary>
        /// <param name="DeletingRows"> Перечень удаляемых строк.</param>
        private bool RefreshDevices(List<BaseCardProperty> DeletingRows = null)
        {
            try
            {
                List<Guid> CardCollection = new List<Guid>();
                List<String> Devices = new List<String>();
                if (DeletingRows.IsNull())
                    CardCollection = Table_Documents.Select(RefAgreementOfDocumentsCard.Documents.DocumentsCard);
                else
                    CardCollection = Table_Documents.Select().Except(DeletingRows).Select(r => r[RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid()).ToList();

                foreach (Guid DocID in CardCollection)
                {
                    CardData DocCard = CardScript.Session.CardManager.GetCardData(DocID);
                    ExtraCardCD CDCard = ExtraCardCD.GetExtraCard(DocCard);
                    if (!CDCard.IsNull())
                        Devices.AddRange(CDCard.Devices);
                    else
                    {
                        ExtraCardTD TDCard = ExtraCardTD.GetExtraCard(DocCard);
                        if (!TDCard.IsNull())
                            Devices.AddRange(TDCard.Devices);
                        else
                            throw new MyException("Не удалось определить тип документа \"" + DocCard.Description + "\".");
                    }
                }
                
                if (!AddDevices(Devices))
                    throw new MyException("Не удалось добавить новые приборы в перечень приборов.");
                if (!RemoveDevices(Devices))
                    throw new MyException("Не удалось удалить лишние приборы из переченя приборов.");
                return true;
            }
            catch (Exception Ex)
            {
                MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        /// <summary>
        /// Обновление записи в таблице "Документы"
        /// </summary>
        /// <param name="DocumentsRow"> Редактируемая строка таблицы.</param>
        /// <param name="DocumentsCardData"> Карточка документа.</param>
        private bool RefreshDocument(BaseCardProperty DocumentsRow, CardData DocumentsCardData)
        {
            try
            {
                ExtraCardCD CDCard = ExtraCardCD.GetExtraCard(DocumentsCardData);
                if (!CDCard.IsNull())
                {
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.CodeID] = CDCard.CodeId;
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsName] = CDCard.Name;
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsCategory] = CDCard.CategoryId;
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsVersion] = CDCard.Version;
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsComment] = "";
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsAuthor] = CDCard.DeveloperID;
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsCard] = CDCard.Card.Id;
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.IsApproved] = false;
                    DocumentsRow[RefAgreementOfDocumentsCard.Documents.ApprovalDate] = null;
                    if (DocumentsRow[RefAgreementOfDocumentsCard.Documents.Id] == null || DocumentsRow[RefAgreementOfDocumentsCard.Documents.Id].ToGuid() == Guid.Empty)
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.Id] = Guid.NewGuid();
                    if (!AddDevices(CDCard.Devices))
                        throw new MyException("Не удалось добавить в перечень новые приборы!");
                }
                else
                {
                    ExtraCardTD TDCard = ExtraCardTD.GetExtraCard(DocumentsCardData);
                    if (!TDCard.IsNull())
                    {
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.CodeID] = TDCard.CodeId;
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsName] = TDCard.Name;
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsCategory] = TDCard.CategoryId;
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsVersion] = TDCard.Version;
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsComment] = "";
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsAuthor] = TDCard.DeveloperID;
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.DocumentsCard] = TDCard.Card.Id;
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.IsApproved] = false;
                        DocumentsRow[RefAgreementOfDocumentsCard.Documents.ApprovalDate] = null;
                        if (DocumentsRow[RefAgreementOfDocumentsCard.Documents.Id] == null || DocumentsRow[RefAgreementOfDocumentsCard.Documents.Id].ToGuid() == Guid.Empty)
                            DocumentsRow[RefAgreementOfDocumentsCard.Documents.Id] = Guid.NewGuid();
                        if (!AddDevices(TDCard.Devices))
                            throw new MyException("Не удалось добавить в перечень новые приборы!");
                    }
                    else
                        throw new MyException("Не удалось определить тип документа!");
                }
                return true;
            }
            catch (MyException Ex) 
            { 
                MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        /// <summary>
        /// Добавление данных в карточку документа
        /// </summary>
        /// <param name="Document"> Карточка документа.</param>
        /// <param name="NewState"> Новое состояние документа.</param>
        private bool AddDataToDocument(CardData Document, DocumentState NewState)
        {
            try
            {
                if (Document.LockStatus != LockStatus.Free)
                    Document.ForceUnlock();

                ExtraCardCD CDocExtraCard = ExtraCardCD.GetExtraCard(Document);
                if (!CDocExtraCard.IsNull())
                    AddDataToDocument(CDocExtraCard, NewState);
                else
                {
                    ExtraCardTD TDocExtraCard = ExtraCardTD.GetExtraCard(Document);
                    if (!TDocExtraCard.IsNull())
                        AddDataToDocument(TDocExtraCard, NewState);
                }
                return true;
            }
            catch (Exception Ex)
            {
                MyMessageBox.Show(Ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        /// <summary>
        /// Удаление данных из карточки документа
        /// </summary>
        /// <param name="Document"> Карточка документа.</param>
        /// <param name="NewState"> Новое состояние документа.</param>
        private bool RemoveDataFromDocument(CardData Document, DocumentState NewState = DocumentState.None)
        {
            try
            {
                if (Document.LockStatus != LockStatus.Free)
                    Document.ForceUnlock();

                ExtraCardCD CDocExtraCard = ExtraCardCD.GetExtraCard(Document);
                if (!CDocExtraCard.IsNull())
                    RemoveDataFromDocument(CDocExtraCard, NewState);
                else
                {
                    ExtraCardTD TDocExtraCard = ExtraCardTD.GetExtraCard(Document);
                    if (!TDocExtraCard.IsNull())
                        RemoveDataFromDocument(TDocExtraCard, NewState);
                }
                return true;
            }
            catch (Exception Ex)
            {
                MyMessageBox.Show(Ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        /// <summary>
        /// Добавление данных в карточку документа
        /// </summary>
        /// <param name="DocExtraCard"> Надстройка для карточки документа.</param>
        /// <param name="NewState"> Новое состояние документа.</param>
        private bool AddDataToDocument(ExtraCard DocExtraCard, DocumentState NewState)
        {
            try
            {
                String NewAction = "";
                String EventComments = "";
                Guid AgreementCardId = Guid.Empty;
                String LinkDescription = "";

                switch (NewState)
                {
                    case DocumentState.OnAgreement:
                        if ((DocumentState)DocExtraCard.Status == DocumentState.Draft)
                        {
                            NewAction = "Отправлен на согласование";
                            EventComments = NewAction + " " + Context.GetObject<StaffEmployee>(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Performer)).GetDisplayString(StaffNameCaseNameCase.Dative);
                            AgreementCardId = CardScript.CardData.Id;
                            LinkDescription = "Согласование";
                        }
                        break;
                    case DocumentState.Approved:
                        NewAction = "Утвержден";
                        EventComments = NewAction + " " + Context.GetObject<StaffEmployee>(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Performer)).GetDisplayString(StaffNameCaseNameCase.Instrumental);
                        break;
                    case DocumentState.Rejected:
                        NewAction = "Отклонен";
                        EventComments = NewAction + " " + Context.GetObject<StaffEmployee>(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Performer)).GetDisplayString(StaffNameCaseNameCase.Instrumental);
                        break;
                    case DocumentState.Rework:
                        break;
                }
                DateTime EventDate = DateTime.Now;
                Guid EventMember = Context.GetObjectRef(CurrentEmployee).ToGuid();
                
                DocExtraCard.Status = NewState;

                if (NewAction != "")
                {
                    if (DocExtraCard.CardType == "Конструкторский документ")
                        ((ExtraCardCD)DocExtraCard).AddHistoryRow(DateTime.Now, NewAction, EventMember, EventComments);
                    if (DocExtraCard.CardType == "Технологический документ")
                        ((ExtraCardTD)DocExtraCard).AddHistoryRow(DateTime.Now, NewAction, EventMember, EventComments);  
                }
                if (AgreementCardId != Guid.Empty)
                {
                    if (DocExtraCard.CardType == "Конструкторский документ")
                        ((ExtraCardCD)DocExtraCard).AddLinkToCard(AgreementCardId, LinkDescription);
                    if (DocExtraCard.CardType == "Технологический документ")
                        ((ExtraCardTD)DocExtraCard).AddLinkToCard(AgreementCardId, LinkDescription);
                }
                return true;
            }
            catch (Exception Ex)
            {
                MyMessageBox.Show(Ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        /// <summary>
        /// Удаление данных из карточки документа
        /// </summary>
        /// <param name="DocExtraCard"> Надстройка для карточки документа.</param>
        /// <param name="NewState"> Новое состояние документа.</param>
        private bool RemoveDataFromDocument(ExtraCard DocExtraCard, DocumentState NewState = DocumentState.None)
        {
            try
            {
                List<String> RemovingActions = new List<string>();
                Guid AgreementCardId = Guid.Empty;

                if (NewState == DocumentState.Draft)
                {
                    RemovingActions.Add("Отправлен на согласование");
                    RemovingActions.Add("Утвержден");
                    RemovingActions.Add("Отклонен");
                    AgreementCardId = CardScript.CardData.Id;
                }
                else
                {
                    switch ((DocumentState)DocExtraCard.Status)
                    {
                        case DocumentState.OnAgreement:
                            NewState = DocumentState.Rework;
                            break;
                        case DocumentState.Approved:
                            NewState = DocumentState.OnAgreement;
                            RemovingActions.Add("Утвержден");
                            break;
                        case DocumentState.Rejected:
                            NewState = DocumentState.OnAgreement;
                            RemovingActions.Add("Отклонен");
                            break;
                        case DocumentState.Rework:
                            NewState = DocumentState.OnAgreement;
                            break;
                    }
                }
                DocExtraCard.Status = NewState;

                if (RemovingActions.Count > 0)
                {
                    if (DocExtraCard.CardType == "Конструкторский документ")
                    {
                        foreach (String Action in RemovingActions)
                        {
                            List<Int32> Index = ((ExtraCardCD)DocExtraCard).FindHistoryRows(RefPropertiesCD.DocumentHistory.Action, Action);
                            if (!Index.IsNull() && Index.Count > 0)
                                ((ExtraCardCD)DocExtraCard).RemoveHistoryRow(Index.Last());
                        }
                    }
                    if (DocExtraCard.CardType == "Технологический документ")
                    {
                        foreach (String Action in RemovingActions)
                        {
                            List<Int32> Index = ((ExtraCardTD)DocExtraCard).FindHistoryRows(RefPropertiesCD.DocumentHistory.Action, Action);
                            if (!Index.IsNull() && Index.Count > 0)
                                ((ExtraCardTD)DocExtraCard).RemoveHistoryRow(Index.Last());
                        }
                    }
                }
                if (AgreementCardId != Guid.Empty)
                {
                    if (DocExtraCard.CardType == "Конструкторский документ")
                        ((ExtraCardCD)DocExtraCard).RemoveLinkFromCard(AgreementCardId);
                    if (DocExtraCard.CardType == "Технологический документ")
                        ((ExtraCardTD)DocExtraCard).RemoveLinkFromCard(AgreementCardId);
                }
                return true;
            }
            catch (Exception Ex)
            {
                MyMessageBox.Show(Ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        #endregion Methods

        #region Event Handlers
        /// <summary>
        /// Добавление строки в таблицу "Документы".
        /// </summary>
        private void AddDocumentButton_ItemClick(Object sender, EventArgs e)
        {
            try
            {
                SearchQuery Query_Search = CardScript.Session.CreateSearchQuery();
                CardTypeQuery Query_CardType = Query_Search.AttributiveSearch.CardTypeQueries.AddNew(CardOrd.ID);
                SectionQuery Query_Section = Query_CardType.SectionQueries.AddNew(CardOrd.MainInfo.ID);
                Query_Section.Operation = SectionQueryOperation.And;
                Query_Section.ConditionGroup.Operation = ConditionGroupOperation.Or;
                Query_Section.ConditionGroup.Conditions.AddNew(CardOrd.MainInfo.Type, FieldType.RefId, ConditionOperation.Equals, MyHelper.RefType_CD);
                Query_Section.ConditionGroup.Conditions.AddNew(CardOrd.MainInfo.Type, FieldType.RefId, ConditionOperation.Equals, MyHelper.RefType_TD);
                Query_Section = Query_CardType.SectionQueries.AddNew(CardOrd.Properties.ID);
                Query_Section.ConditionGroup.Operation = ConditionGroupOperation.And;
                Query_Section.ConditionGroup.Conditions.AddNew(CardOrd.Properties.Name, FieldType.Unistring, ConditionOperation.Equals, "Статус");
                Query_Section.ConditionGroup.Conditions.AddNew(CardOrd.Properties.Value, FieldType.Int, ConditionOperation.Equals, DocumentState.Draft);

                Guid[] DocIds = CardScript.CardFrame.CardHost.SelectCards("Выберите документы для согласования...", RefPreSelectedFolderForDocuments, Query_Search.GetXml());
                List<MyException> ErrorsList = new List<MyException>();
                if (DocIds.Count() > 0)
                {
                    foreach (Guid DocId in DocIds)
                    {
                        if (!DocId.IsEmpty())
                        {
                            CardData DocData = CardScript.Session.CardManager.GetCardData(DocId);

                            if (!CardScript.Session.AccessManager.AccessCheck(SecureObjectType.Card, DocId, Guid.Empty, (Int32)(CardDataRights.Read | CardDataRights.Modify | CardDataRights.Copy)))
                            {
                                ErrorsList.Add(new MyException(0, DocData.Description));
                                //ErrorsList.Add(DocData.Description + ": У вас нет прав на редактирование выбранного документа!"); // throw new MyException("У вас нет прав на редактирование выбранного документа!");
                            }
                            else
                            {
                                if (DocData.Type.Id == CardOrd.ID)
                                {
                                    Guid DocType = DocData.Sections[CardOrd.MainInfo.ID].FirstRow.GetGuid(CardOrd.MainInfo.Type) ?? Guid.Empty;
                                    if (DocType == MyHelper.RefType_CD || DocType == MyHelper.RefType_TD)
                                    {
                                        if ((Int32?)DocData.Sections[CardOrd.Properties.ID].GetPropertyValue("Статус") == (Int32)DocumentState.Draft)
                                        {
                                            if (!Table_Documents.Select(RefAgreementOfDocumentsCard.Documents.DocumentsCard).Any(r => r == DocId))
                                            {
                                                AddDocument(DocData);
                                                Table_Documents.RefreshRows();
                                                if (Table_Documents.RowCount != 0)
                                                    ICardControl.DisableTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 4, false);
                                            }
                                            else
                                                ErrorsList.Add(new MyException(1, DocData.Description));   //ErrorsList.Add(DocData.Description + ": Выбраный документ уже добавлен в текущую карточку согласования!"); // throw new MyException("Выбраный документ уже добавлен в текущую карточку согласования!");
                                        }
                                        else
                                            ErrorsList.Add(new MyException(2, DocData.Description));   // ErrorsList.Add(DocData.Description + ": Выбраный документ не является черновиком!"); // throw new MyException("Выбраный документ не является черновиком!");
                                    }
                                    else
                                        ErrorsList.Add(new MyException(3, DocData.Description)); // ErrorsList.Add(DocData.Description + ": Выбран неверный тип документа!"); // throw new MyException("Выбран неверный тип документа!");
                                }
                                else
                                    ErrorsList.Add(new MyException(4, DocData.Description));  // ErrorsList.Add(DocData.Description + ": Выбран неверный тип документа!"); // throw new MyException("Выбран неверный тип документа!");
                            }
                        }
                    }
                    if (ErrorsList.Count() > 0)
                    {
                        if (DocIds.Count() == 1)
                            throw new MyException(GetErrorText(ErrorsList.First().ErrorCode, 1) + ".");
                        else
                            throw new MyException(ErrorsList.GroupBy(r => r.ErrorCode).Select(r => GetErrorText(r.First().ErrorCode, r.Count()) + ":\n" + r.Select(s => " - " + s.Message).Aggregate(";\n") + ".").Aggregate("\n") + "\n" + ErrorsList.Count().GetCaseString("Он не будет добавлен", "Оба вышеуказанных документа не будут добавлены", "Все вышеперечисленные документы не будут добавлены") + " в таблицу \"Документы\".");
                    }
                }
            }
            catch (MyException Ex) { MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        /// <summary>
        /// Получение текста ошибки по коду.
        /// </summary>
        /// <param name="ErrorCode"> Код ошибки.</param>
        /// <param name="ErrorCount"> Количество ошибок.</param>
        /// <returns></returns>
        private string GetErrorText(Int32 ErrorCode, Int32 ErrorCount)
        {
            switch (ErrorCode)
            {
                case 0:
                    return "У вас нет прав на редактирование " + ErrorCount.GetCaseString("данного документа", "данных документов", "данных документов");
                case 1:
                    return ErrorCount.GetCaseString("Данный документ уже добавлен", "Данные документы уже добавлены", "Данные документы уже добавлены") + " в текущую карточку согласования";
                case 2:
                    return ErrorCount.GetCaseString("Данный документ не является черновиком", "Данные документы не являются черновиками", "Данные документы не являются черновиками");
                case 3:
                case 4:
                    return ErrorCount.GetCaseString("Данный документ имеет", "Данные документы имеют", "Данные документы имеют") + " неверный тип";
                default:
                    return String.Empty;
            }
        }
        /// <summary>
        /// Удаление строки из таблицы "Документы"
        /// </summary>
        private void RemoveDocumentButton_ItemClick(Object sender, EventArgs e)
        {
            try
            {
                List<BaseCardProperty> DeletingRows = new List<BaseCardProperty>();
                foreach (int RowIndex in Grid_Documents.GetSelectedRows())
                    DeletingRows.Add(Table_Documents[RowIndex]);

                if (!RefreshDevices(DeletingRows))
                    throw new MyException("Не удалось обновить перечень приборов.");

                foreach (BaseCardProperty DeletingRow in DeletingRows)
                {
                    RemovedDocuments.Add(DeletingRow[RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid());
                    RemoveDocument(DeletingRow);
                    if (DeletingRow[RefAgreementOfDocumentsCard.Documents.RefRowId] != null && DeletingRow[RefAgreementOfDocumentsCard.Documents.RefRowId].ToGuid() != Guid.Empty)
                        SynchronizeRows.Add(DeletingRow[RefAgreementOfDocumentsCard.Documents.RefRowId].ToGuid());
                }
                if (Table_Documents.RowCount == 0)
                    ICardControl.DisableTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 4, true);
            }
            catch (MyException Ex) { MyMessageBox.Show(Ex.Message, "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        /// <summary>
        /// Открытие карточки документа
        /// </summary>
        private void OpenCardDocumentButton_ItemClick(Object sender, EventArgs e)
        {
            try
            {
                foreach (int RowIndex in Grid_Documents.GetSelectedRows())
                {
                    Guid CardId = Table_Documents[RowIndex][RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid();
                    if (CardId.IsNull() || CardId.IsEmpty())
                        throw new MyException("Карточка документа не найдена.");
                    CardScript.CardFrame.CardHost.ShowCard(CardId, DocsVision.Platform.CardHost.ActivateMode.Edit);
                }
            }
            catch (MyException Ex) { MyMessageBox.Show(Ex.Message, "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        /// <summary>
        /// Открытие папки с документами
        /// </summary>
        private void OpenFolderDocumentButton_ItemClick(Object sender, EventArgs e)
        {
            try
            {
                foreach (int RowIndex in Grid_Documents.GetSelectedRows())
                {
                    BaseCardProperty Row = Table_Documents[RowIndex];
                    ExtraCardCD DocumentCard = ExtraCardCD.GetExtraCard(CardScript.Session.CardManager.GetCardData(Row[RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid()));
                    if (!Directory.Exists(DocumentCard.Path))
                        throw new MyException("Папка с файлами документа не найдена.");
                    Process.Start("explorer", DocumentCard.Path);
                }
            }
            catch (MyException Ex) { MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        /// <summary>
        /// Двойной клик по таблице "Документы"
        /// </summary>
        private void Documents_DoubleClick(Object sender, EventArgs e)
        {
            try
            {
                String CurrentState = CardScript.BaseObject.SystemInfo.State.DefaultName;
                switch (CurrentState)
                {
                    case RefAgreementOfDocumentsCard.CardState.NotStarted:
                    case RefAgreementOfDocumentsCard.CardState.InReworking:
                        if (CurrentState == RefAgreementOfDocumentsCard.CardState.InReworking && (bool)Table_Documents[Table_Documents.FocusedRowIndex][RefAgreementOfDocumentsCard.Documents.IsApproved])
                            throw new MyException("Вы не можете изменить утвержденный документ.");

                        SearchQuery Query_Search = CardScript.Session.CreateSearchQuery();
                        CardTypeQuery Query_CardType = Query_Search.AttributiveSearch.CardTypeQueries.AddNew(CardOrd.ID);
                        SectionQuery Query_Section = Query_CardType.SectionQueries.AddNew(CardOrd.MainInfo.ID);
                        Query_Section.Operation = SectionQueryOperation.And;
                        Query_Section.ConditionGroup.Operation = ConditionGroupOperation.Or;
                        Query_Section.ConditionGroup.Conditions.AddNew(CardOrd.MainInfo.Type, FieldType.RefId, ConditionOperation.Equals, MyHelper.RefType_CD);
                        Query_Section.ConditionGroup.Conditions.AddNew(CardOrd.MainInfo.Type, FieldType.RefId, ConditionOperation.Equals, MyHelper.RefType_TD);
                        Query_Section = Query_CardType.SectionQueries.AddNew(CardOrd.Properties.ID);
                        Query_Section.ConditionGroup.Operation = ConditionGroupOperation.And;
                        Query_Section.ConditionGroup.Conditions.AddNew(CardOrd.Properties.Name, FieldType.Unistring, ConditionOperation.Equals, "Статус");
                        Query_Section.ConditionGroup.Conditions.AddNew(CardOrd.Properties.Value, FieldType.Int, ConditionOperation.Equals, 1);

                        Guid DocId = CardScript.CardFrame.CardHost.SelectCard("Выберите документ для согласования...", RefPreSelectedFolderForDocuments, Query_Search.GetXml());
                        List<MyException> ErrorsList = new List<MyException>();
                        if (!DocId.IsEmpty())
                        {
                            CardData DocData = CardScript.Session.CardManager.GetCardData(DocId);
                            if (!CardScript.Session.AccessManager.AccessCheck(SecureObjectType.Card, DocId, Guid.Empty, (Int32)(CardDataRights.Read | CardDataRights.Modify | CardDataRights.Copy)))
                            {
                                ErrorsList.Add(new MyException(0, DocData.Description)); // throw new MyException("У вас нет прав на редактирование выбранного документа!");
                            }
                            else
                            {
                                if (DocData.Type.Id == CardOrd.ID)
                                {
                                    Guid DocType = DocData.Sections[CardOrd.MainInfo.ID].FirstRow.GetGuid(CardOrd.MainInfo.Type) ?? Guid.Empty;
                                    if (DocType == MyHelper.RefType_CD || DocType == MyHelper.RefType_TD)
                                    {
                                        if ((Int32?)DocData.Sections[CardOrd.Properties.ID].GetPropertyValue("Статус") == (Int32)DocumentState.Draft)
                                        {
                                            if (!Table_Documents.Select(RefAgreementOfDocumentsCard.Documents.DocumentsCard).Any(r => r == DocId))
                                            {
                                                RefreshDocument(Table_Documents[Table_Documents.FocusedRowIndex], DocData);
                                                RefreshDevices();
                                                Table_Documents.RefreshRows();
                                            }
                                            else
                                                ErrorsList.Add(new MyException(1, DocData.Description)); // throw new MyException("Выбраный документ уже добавлен в текущую карточку согласования!");
                                        }
                                        else
                                            ErrorsList.Add(new MyException(2, DocData.Description)); // throw new MyException("Выбранный документ не является черновиком!");
                                    }
                                    else
                                        ErrorsList.Add(new MyException(3, DocData.Description)); // throw new MyException("Выбран неверный тип документа!");
                                }
                                else
                                    ErrorsList.Add(new MyException(4, DocData.Description)); // throw new MyException("Выбран неверный тип документа!");
                            }
                            if (ErrorsList.Count() > 0)
                                throw new MyException(GetErrorText(ErrorsList.First().ErrorCode, 1) + ".");
                        }
                        break;
                    case RefAgreementOfDocumentsCard.CardState.InSimpleAgreement:
                    case RefAgreementOfDocumentsCard.CardState.InSmartAgreement:
                        if ((Grid_Documents.FocusedColumn.Name != RefAgreementOfDocumentsCard.Documents.IsApproved) && (Grid_Documents.FocusedColumn.Name != RefAgreementOfDocumentsCard.Documents.ApprovalDate))
                        {
                            CardData DocCard = CardScript.Session.CardManager.GetCardData(Table_Documents[Table_Documents.FocusedRowIndex][RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid());
                            ExtraCard DocExtraCard = ExtraCardCD.GetExtraCard(DocCard);
                            if (DocExtraCard.IsNull())
                                DocExtraCard = ExtraCardTD.GetExtraCard(DocCard);
                            if (!DocExtraCard.IsNull())
                            {
                                if (!Directory.Exists(DocExtraCard.Path))
                                    throw new MyException("Папка с файлами документа не найдена.");
                                Process.Start("explorer", DocExtraCard.Path);
                            }
                        }
                        break;
                }
            }
            catch (MyException Ex) { MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        /// <summary>
        /// Изменение поля "Состояние"
        /// </summary>
        private void State_EditValueChanged(Object sender, EventArgs e)
        {
            try
            {
                /*int Status = (int)GetControlValue(RefAgreementOfDocumentsCard.MainInfo.State);
                switch (Status)
                {
                    case (int)RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.NotStarted:
                        CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.NotStarted);
                        break;
                    case (int)RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.InAgreement:
                        if (PartiallyApproval)
                            CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InSmartAgreement);
                        else
                            CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InSimpleAgreement);
                        break;
                    case (int)RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.InReworking:
                        CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InReworking);
                        break;
                    case (int)RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.Approved:
                    case (int)RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.PartiallyApproved:
                    case (int)RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.Rejected:
                        CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.Completed);
                        break;
                }
                CardScript.SaveCard();*/
            }
            catch (Exception Ex) { MyMessageBox.Show(Ex.Message); }
        }
        /// <summary>
        /// Изменение поля "Возможно частичное утверждение"
        /// </summary>
        private void PartiallyApproval_EditValueChanged(Object sender, EventArgs e)
        {
            try
            {
                string CurrentState = CardScript.BaseObject.SystemInfo.State.DefaultName;
                bool PartiallyApproval = ((bool?)GetControlValue(RefAgreementOfDocumentsCard.MainInfo.PartiallyApproval)) ?? false;

                if (PartiallyApproval && CurrentState == RefAgreementOfDocumentsCard.CardState.InSimpleAgreement)
                {
                    NewHistoryRow(DateTime.Now, CurrentEmployee, "Разрешено частичное утверждение документов.");
                    this.CardScript.CardData.Sections[RefAgreementOfDocumentsCard.MainInfo.ID].FirstRow.SetBoolean(RefAgreementOfDocumentsCard.MainInfo.PartiallyApproval, PartiallyApproval);
                    CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InSmartAgreement);
                }
                if (!PartiallyApproval && CurrentState == RefAgreementOfDocumentsCard.CardState.InSmartAgreement)
                {
                    NewHistoryRow(DateTime.Now, CurrentEmployee, "Запрещено частичное утверждение документов.");
                    this.CardScript.CardData.Sections[RefAgreementOfDocumentsCard.MainInfo.ID].FirstRow.SetBoolean(RefAgreementOfDocumentsCard.MainInfo.PartiallyApproval, PartiallyApproval);
                    CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InSimpleAgreement);
                }
            }
            catch (Exception Ex) { MessageBox.Show(Ex.Message); }
        }
        /// <summary>
        /// Команда "Отправить".
        /// </summary>
        private void Send_ItemClick(Object sender, ItemClickEventArgs e)
        {
            try
            {
                // ============================
                // === Выполняются проверки ===
                // ============================

                // Если в таблице "Документы" нет ни одной записи, отправка отменяется.
                if (Table_Documents.RowCount == 0)
                    throw new MyException("Укажите хотя бы один документ для согласования.");

                // Если не заполнено поле "Исполнитель", отправка отменяется.
                if (GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Performer).IsNull() || GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Performer).ToGuid().Equals(Guid.Empty))
                    throw new MyException("Укажите согласователя.");

                // Если в таблице "Документы" пустая строка
                for (int i = 0; i < Table_Documents.RowCount; i++)
                {
                    if (Table_Documents[i][RefAgreementOfDocumentsCard.Documents.DocumentsCard].IsNull())
                        throw new MyException("Не выбран документ в таблице \"Документы\"");
                }

                // ===============================
                // === Осуществляется отправка ===
                // ===============================

                // Изменение состояния документов
                foreach (CardData DocumentCard in Table_Documents.Select(RefAgreementOfDocumentsCard.Documents.DocumentsCard).Select(r => CardScript.Session.CardManager.GetCardData(r)))
                    AddDataToDocument(DocumentCard, DocumentState.OnAgreement);

                if (CardScript.BaseObject.SystemInfo.State.DefaultName == RefAgreementOfDocumentsCard.CardState.NotStarted)
                    SetControlValue(RefAgreementOfDocumentsCard.MainInfo.SentDate, DateTime.Now);

                StaffEmployee PerformerEmployee = Context.GetObject<StaffEmployee>(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Performer).ToGuid());
                NewHistoryRow(DateTime.Now, CurrentEmployee, "Документы отправлены на согласование " + PerformerEmployee.GetDisplayString(StaffNameCaseNameCase.Dative));
                SetControlValue(RefAgreementOfDocumentsCard.MainInfo.State, RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.InAgreement);

                if ((bool)GetControlValue(RefAgreementOfDocumentsCard.MainInfo.PartiallyApproval))
                    CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InSmartAgreement);
                else
                    CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InSimpleAgreement);

                CardScript.SaveCard();
                MyMessageBox.Show("Документы успешно отправлены на согласование.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.CardScript.CardControl.Close();
            }
            catch (MyException Ex) { MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception Ex) { MyMessageBox.Show(Ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        /// <summary>
        /// Команда "Делегировать".
        /// </summary>
        private void Delegate_ItemClick(Object sender, ItemClickEventArgs e)
        {
            try
            {
                // ====================================
                // === Осуществляется делегирование ===
                // ====================================

                SimpleDelegateForm NewDelegateForm = new SimpleDelegateForm(CardScript.CardFrame.CardHost, Context);
                NewDelegateForm.ShowDialog();
                if (NewDelegateForm.DialogResult == DialogResult.OK)
                {
                    string CommentToDelegate = "Согласование делегировано " + NewDelegateForm.Delegate.GetDisplayString(StaffNameCaseNameCase.Dative) + " Комментарий: " + NewDelegateForm.Comment;
                    NewHistoryRow(DateTime.Now, CurrentEmployee, CommentToDelegate);
                    SetControlValue(RefAgreementOfDocumentsCard.MainInfo.Performer, Context.GetObjectRef(NewDelegateForm.Delegate).Id);
                    CardScript.SaveCard();
                    this.CardScript.CardControl.Close();
                }
            }
            catch (Exception Ex) { MyMessageBox.Show(Ex.Message); }
        }
        /// <summary>
        /// Команда "Вернуть на доработку"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Return_ItemClick(Object sender, ItemClickEventArgs e)
        {
            try
            {
                if (GetControlValue(RefAgreementOfDocumentsCard.MainInfo.PerformersComment).IsNull() || String.IsNullOrEmpty(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.PerformersComment).ToString()))
                    throw new MyException("Укажите свои замечания в поле \"Комментарий согласователя\".");
                else
                {
                    foreach (CardData DocumentCard in Table_Documents.Select(RefAgreementOfDocumentsCard.Documents.DocumentsCard).Select(r => CardScript.Session.CardManager.GetCardData(r)))
                        AddDataToDocument(DocumentCard, DocumentState.Rework);
                    StaffEmployee CreatorEmployee = Context.GetObject<StaffEmployee>(GetControlValue(RefAgreementOfDocumentsCard.MainInfo.Registrar).ToGuid());
                    NewHistoryRow(DateTime.Now, CurrentEmployee, "Документы возвращены на доработку " + CreatorEmployee.GetDisplayString(StaffNameCaseNameCase.Dative));
                    SetControlValue(RefAgreementOfDocumentsCard.MainInfo.State, RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.InReworking);
                    CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InReworking);
                    CardScript.SaveCard();
                    this.CardScript.CardControl.Close();
                }
            }
            catch (MyException Ex) { MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception Ex) { MyMessageBox.Show(Ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        /// <summary>
        /// Команда "Утвердить все документы"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Approve_ItemClick(Object sender, ItemClickEventArgs e)
        {
            try
            {
                for (int i = 0; i < Table_Documents.RowCount; i++)
                {
                    Table_Documents[i][RefAgreementOfDocumentsCard.Documents.IsApproved] = true;
                    Table_Documents[i][RefAgreementOfDocumentsCard.Documents.ApprovalDate] = DateTime.Today;
                    CardData DocumentCard = CardScript.Session.CardManager.GetCardData(Table_Documents[i][RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid());
                    AddDataToDocument(DocumentCard, DocumentState.Approved);
                }

                NewHistoryRow(DateTime.Now, CurrentEmployee, "Все документы утверждены " + CurrentEmployee.GetDisplayString(StaffNameCaseNameCase.Instrumental));
                SetControlValue(RefAgreementOfDocumentsCard.MainInfo.EndDate, DateTime.Now);
                SetControlValue(RefAgreementOfDocumentsCard.MainInfo.State, RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.Approved);
                CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.Completed);
                CardScript.SaveCard();
                this.CardScript.CardControl.Close();
            }
            catch (Exception Ex) { MyMessageBox.Show(Ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        /// <summary>
        /// Команда "Утвердить только отмеченные документы"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApproveChecked_ItemClick(Object sender, ItemClickEventArgs e)
        {
            try
            {
                if (!Table_Documents.Select<bool>(RefAgreementOfDocumentsCard.Documents.IsApproved).Any(r => r))
                    throw new MyException("Отметьте хотя бы один документ для утверждения.");

                for (int i = 0; i < Table_Documents.RowCount; i++)
                {
                    CardData DocumentCard = CardScript.Session.CardManager.GetCardData(Table_Documents[i][RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid());
                    if ((bool)Table_Documents[i][RefAgreementOfDocumentsCard.Documents.IsApproved])
                    {
                        AddDataToDocument(DocumentCard, DocumentState.Approved);
                        Table_Documents[i][RefAgreementOfDocumentsCard.Documents.ApprovalDate] = DateTime.Today;
                    }
                    else
                    {
                        AddDataToDocument(DocumentCard, DocumentState.Rejected);
                    }
                }

                NewHistoryRow(DateTime.Now, CurrentEmployee, "Отмеченные документы утверждены " + CurrentEmployee.GetDisplayString(StaffNameCaseNameCase.Instrumental));
                SetControlValue(RefAgreementOfDocumentsCard.MainInfo.EndDate, DateTime.Now);
                SetControlValue(RefAgreementOfDocumentsCard.MainInfo.State, RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.PartiallyApproved);
                CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.Completed);
                CardScript.SaveCard();
                this.CardScript.CardControl.Close();
            }
            catch (MyException Ex) { MyMessageBox.Show(Ex.Message, "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception Ex) { MyMessageBox.Show(Ex.Message, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        /// <summary>
        /// Команда "Отклонить все документы"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reject_ItemClick(Object sender, ItemClickEventArgs e)
        {
            try
            {
                for (int i = 0; i < Table_Documents.RowCount; i++)
                {
                    Table_Documents[i][RefAgreementOfDocumentsCard.Documents.IsApproved] = false;
                    CardData DocumentCard = CardScript.Session.CardManager.GetCardData(Table_Documents[i][RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid());
                    AddDataToDocument(DocumentCard, DocumentState.Rejected);
                }

                NewHistoryRow(DateTime.Now, CurrentEmployee, "Все документы отклонены " + CurrentEmployee.GetDisplayString(StaffNameCaseNameCase.Instrumental));
                SetControlValue(RefAgreementOfDocumentsCard.MainInfo.EndDate, DateTime.Now);
                SetControlValue(RefAgreementOfDocumentsCard.MainInfo.State, RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.Rejected);
                CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.Completed);
                CardScript.SaveCard();
                this.CardScript.CardControl.Close();
            }
            catch (Exception Ex) { MyMessageBox.Show(Ex.Message); }
        }
        /// <summary>
        /// Команда "Вернуть".
        /// </summary>
        private void Revoke_ItemClick(Object sender, ItemClickEventArgs e)
        {
            try
            {
                // ============================================================
                // === Осуществляется возврат согласования в предыдущее состояние ===
                // ============================================================

                string CurrentState = CardScript.BaseObject.SystemInfo.State.DefaultName;
                switch (CurrentState)
                {
                    case RefAgreementOfDocumentsCard.CardState.InSimpleAgreement:
                    case RefAgreementOfDocumentsCard.CardState.InSmartAgreement:
                        if (Table_History.SelectObject<String>(RefAgreementOfDocumentsCard.AgreementHistory.EventDescription).Any(r => r.StartsWith("Документы возвращены на доработку")))
                        {
                            SetControlValue(RefAgreementOfDocumentsCard.MainInfo.State, RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.InReworking);
                            CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InReworking);
                            foreach (CardData DocumentCard in Table_Documents.Select(RefAgreementOfDocumentsCard.Documents.DocumentsCard).Select(r => CardScript.Session.CardManager.GetCardData(r)))
                                RemoveDataFromDocument(DocumentCard);
                        }
                        else
                        {
                            SetControlValue(RefAgreementOfDocumentsCard.MainInfo.State, RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.NotStarted);
                            CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.NotStarted);
                            foreach (CardData DocumentCard in Table_Documents.Select(RefAgreementOfDocumentsCard.Documents.DocumentsCard).Select(r => CardScript.Session.CardManager.GetCardData(r)))
                                RemoveDataFromDocument(DocumentCard, DocumentState.Draft);
                        }
                        NewHistoryRow(DateTime.Now, CurrentEmployee, "Согласование возвращено в состояние \"" + Edit_State.SelectedItem.ToString() + "\".");
                        break;
                    case RefAgreementOfDocumentsCard.CardState.InReworking:
                    case RefAgreementOfDocumentsCard.CardState.Completed:
                        SetControlValue(RefAgreementOfDocumentsCard.MainInfo.State, RefAgreementOfDocumentsCard.MainInfo.DisplayCardState.InAgreement);
                        if ((bool)GetControlValue(RefAgreementOfDocumentsCard.MainInfo.PartiallyApproval))
                            CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InSmartAgreement);
                        else
                            CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.InSimpleAgreement);
                        SetControlValue(RefAgreementOfDocumentsCard.MainInfo.EndDate, null);
                        NewHistoryRow(DateTime.Now, CurrentEmployee, "Согласование возвращено в состояние \"" + Edit_State.SelectedItem.ToString() + "\".");
                        for (int i = 0; i < Table_Documents.RowCount; i++)
                        {
                            CardData DocumentCard =  CardScript.Session.CardManager.GetCardData(Table_Documents[i][RefAgreementOfDocumentsCard.Documents.DocumentsCard].ToGuid());
                            RemoveDataFromDocument(DocumentCard);
                            Table_Documents[i][RefAgreementOfDocumentsCard.Documents.ApprovalDate] = null;
                        }
                        break;
                    case RefAgreementOfDocumentsCard.CardState.Closed:
                        CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.Completed);
                        NewHistoryRow(DateTime.Now, CurrentEmployee, "Согласование возвращено в состояние \"" + Edit_State.SelectedItem.ToString() + "\".");
                        break;
                }
                CardScript.SaveCard();
                this.CardScript.CardControl.Close();
            }
            catch (Exception Ex) { MessageBox.Show(Ex.Message); }
        }
        /// <summary>
        /// Команда "Закрыть"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_ItemClick(Object sender, ItemClickEventArgs e)
        {
            try
            {
                // ============================================
                // === Осуществляется закрытие согласования ===
                // ============================================
                CardScript.ChangeState(RefAgreementOfDocumentsCard.CardState.Closed);
                NewHistoryRow(DateTime.Now, CurrentEmployee, "Автор ознакомлен с результатами согласования.");
                CardScript.SaveCard();
                this.CardScript.CardControl.Close();
            }
            catch (Exception Ex) { MessageBox.Show(Ex.Message); }
        }
        /// <summary>
        /// Перед сохранением карточки.
        /// </summary>
        private void CardControl_Saving(Object sender, CancelEventArgs e)
        {
            try 
            {
                for (int i = 0; i < SynchronizeRows.Count(); i++)
                {
                    SearchQuery searchQuery = CardScript.Session.CreateSearchQuery();
                    CardTypeQuery typeQuery = searchQuery.AttributiveSearch.CardTypeQueries.AddNew(RefChangesInDocumentationCard.ID);
                    SectionQuery sectionQuery = typeQuery.SectionQueries.AddNew(RefChangesInDocumentationCard.ChangesInDocs.ID);
                    sectionQuery.ConditionGroup.Conditions.AddNew(RefChangesInDocumentationCard.ChangesInDocs.Id, FieldType.RefId, ConditionOperation.Equals, SynchronizeRows[i]);
                    searchQuery.Limit = 0;
                    String query = searchQuery.GetXml(null, true);
                    CardDataCollection FindCards = CardScript.Session.CardManager.FindCards(query);
                    if (FindCards.Count() > 1)
                        throw new MyException("Невозможно произвести синхронизацию данных со связанной карточкой \"Изменения в документации\". Обнаружено несколько карточек \"Изменения в документации\" для одного документа.");
                    if (FindCards.Count() == 0)
                        throw new MyException("Невозможно произвести синхронизацию данных со связанной карточкой \"Изменения в документации\". Связанная Карточка \"Изменения в документации\" Не обнаружена.");
                    if (FindCards.Count() == 1 && FindCards.First().LockStatus != LockStatus.Free)
                        throw new MyException("Невозможно произвести синхронизацию данных со связанной карточкой \"" + FindCards.First().Description + "\". Карточка заблокирована.");
                }
                for (int i = 0; i < SynchronizeRows.Count(); i++)
                {
                    SearchQuery searchQuery = CardScript.Session.CreateSearchQuery();
                    CardTypeQuery typeQuery = searchQuery.AttributiveSearch.CardTypeQueries.AddNew(RefChangesInDocumentationCard.ID);
                    SectionQuery sectionQuery = typeQuery.SectionQueries.AddNew(RefChangesInDocumentationCard.ChangesInDocs.ID);
                    sectionQuery.ConditionGroup.Conditions.AddNew(RefChangesInDocumentationCard.ChangesInDocs.Id, FieldType.RefId, ConditionOperation.Equals, SynchronizeRows[i]);
                    searchQuery.Limit = 0;
                    String query = searchQuery.GetXml(null, true);
                    CardDataCollection FindCards = CardScript.Session.CardManager.FindCards(query);
                    RowData RefRow = FindCards.First().Sections[RefChangesInDocumentationCard.ChangesInDocs.ID].GetRow(SynchronizeRows[i]);
                    RefRow.SetGuid("AgreementID", null);
                    RefRow.SetGuid("AgreementRowID", null);
                }
                for (int i = 0; i < RemovedDocuments.Count(); i++)
                    RemoveDataFromDocument(CardScript.Session.CardManager.GetCardData(RemovedDocuments[i]), DocumentState.Draft);
            }
            catch (Exception Ex)
            {
                CallError(Ex);
                MessageBox.Show(Ex.Message);
            }
        }
        /// <summary>
        /// Сохранение карточки.
        /// </summary>
        private void CardControl_Saved(Object sender, EventArgs e)
        {
            try
            {
                StringBuilder Digest = new StringBuilder("Согласование документов №");
                if (CurrentNumerator.IsNull())
                    Digest.Append("<не указан>");
                else
                    Digest.Append(CurrentNumerator.Number);
                WriteLog("Формируем дайджест");
                DateTime CreationDate = (DateTime)GetControlValue(RefAgreementOfDocumentsCard.MainInfo.CreationDate);
                WriteLog("Дата регистрации карточки: " + CreationDate.ToShortDateString());
                Digest.Append(" от " + CreationDate.ToShortDateString());
                CardScript.UpdateDescription(Digest.ToString());
                WriteLog("Сформировали дайджест:" + Digest.ToString());
            }
            catch (Exception Ex) 
            { 
                CallError(Ex);
                MessageBox.Show(Ex.Message);
            }
        }
        /// <summary>
        /// Закрытие карточки.
        /// </summary>
        private void CardControl_CardClosed(Object sender, EventArgs e)
        {
            try
            {
                /* Отвязка методов */
                ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Send].ItemClick -= Send_ItemClick;
                ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Delegate].ItemClick -= Delegate_ItemClick;
                ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Approve].ItemClick -= Approve_ItemClick;
                ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.ApproveChecked].ItemClick -= ApproveChecked_ItemClick;
                ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Return].ItemClick -= Return_ItemClick;
                ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Reject].ItemClick -= Reject_ItemClick;
                ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Revoke].ItemClick -= Revoke_ItemClick;
                ICardControl.RibbonControl.Items[RefAgreementOfDocumentsCard.RibbonItems.Close].ItemClick -= Close_ItemClick;
                CardScript.CardControl.Saving -= CardControl_Saving;
                CardScript.CardControl.Saved -= CardControl_Saved;
                CardScript.CardControl.CardClosed -= CardControl_CardClosed;

                ICardControl.RemoveTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 6);
                ICardControl.RemoveTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 5);
                ICardControl.RemoveTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 4);
                ICardControl.RemoveTableBarItem(RefAgreementOfDocumentsCard.Documents.Alias, 3);

                if (FolderCard.GetShortcuts(CardScript.CardData.Id).Count == 0)
                {
                    try { CardScript.ReleaseNumber(CurrentNumerator.NumericPart); }
                    catch { WriteLog("Не удалось освободить номер!"); }
                }
            }
            catch (Exception Ex) { CallError(Ex); }
        }
        #endregion Event Handlers
    }
}