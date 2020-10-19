using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraLayout.Utils;
using DocsVision.BackOffice.CardLib.CardDefs;
using DocsVision.BackOffice.ObjectModel;
using DocsVision.BackOffice.WinForms;
using DocsVision.BackOffice.WinForms.Controls;
using DocsVision.BackOffice.WinForms.Design.PropertyControls;
using DocsVision.Platform.CardHost;
using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectManager.Metadata;
using DocsVision.Platform.ObjectManager.SearchModel;
using DocsVision.Platform.WinForms;
using RKIT.MyCollectionControl.Design.Control;
using RKIT.MyMessageBox;
using SKB.Archive;
using SKB.Base;
using SKB.Base.Dictionary;
using SKB.Base.Ref;
using SKB.Base.Services;
using SKB.Base.Synchronize;

namespace SKB.Archive.Cards
{
    /// <summary>
    /// Карточка "Файлы Маркетинга".
    /// </summary>
    public class MarketingFilesCard : MyBaseCard, IUploadingCard
    {
        #region Properties
        /* КОНТРОЛЫ ДЛЯ ВЫГРУЗКИ ФАЙЛОВ В АРХИВНУЮ ПАПКУ НА СЕРВЕРЕ */
        /// <summary>
        /// Коллекционный контрол категорий карточки
        /// </summary>
        public CollectionControlView Control_Categories
        {
            get { return _Control_Categories; }
        }
        /// <summary>
        /// Кнопка "Загрузить файлы"
        /// </summary>
        public SimpleButton Button_Upload
        {
            get { return _Button_Upload; }
        }
        /// <summary>
        /// Поле "Файлы"
        /// </summary>
        public TextEdit Edit_Files
        {
            get { return _Edit_Files; }
        }
        /// <summary>
        /// Поле "Папка"
        /// </summary>
        public TextEdit Edit_Folder
        {
            get { return ICardControl.FindPropertyItem<TextEdit>(RefMarketingFilesCard.MainInfo.Folder); }
        }
        /* СТРУКТУРА АРХИВА НА СЕРВЕРЕ */
        /// <summary>
        /// Полный путь к архивной папке на сервере
        /// </summary>
        public String FolderPath
        {
            get { return (GetControlValue(RefMarketingFilesCard.MainInfo.Folder) ?? String.Empty).ToString(); }
            set { SetControlValue(RefMarketingFilesCard.MainInfo.Folder, value); }
        }
        /// <summary>
        /// Название раздела в архиве на сервере
        /// </summary>
        public string ArchiveSection
        {
            get { return RefMarketingFilesCard.Name; }
        }
        /// <summary>
        /// Название подраздела в архиве на сервере.
        /// </summary>
        public string ArchiveSubSection
        {
            get { return CardScript.BaseObject.SystemInfo.CardKind.Name; }
        }
        /// <summary>
        /// Название регистрационной папки на сервере.
        /// </summary>
        public String FolderName
        {
            get { return (_Control_Categories.SelectedItems.Any() ? Context.GetObject<CategoriesCategory>(_Control_Categories.SelectedItems[0].ObjectId).Name : String.Empty) + ". " + Changes[RefMarketingFilesCard.MainInfo.Name].NewValue; }
        }
        ///
        /// СИНХРОНИЗАЦИЯ РЕКВИЗИТОВ КАРТОЧКИ С АРХИВНОЙ ПАПКОЙ НА СЕРВЕРЕ
        /// 
        /// <summary>
        /// Перечень изменений карточки (Key - название свойства, Value - данные об изменении свойства)
        /// </summary>
        public Dictionary<String, ChangingValue<String>> Changes { get; set; }
        /// <summary>
        /// Требуется синхронизация реквизитов карточки с архивной папкой на сервере
        /// </summary>
        public Boolean NeedSynchronize { get; set; }
        /* РЕЖИМЫ ОТКРЫТИЯ КАРТОЧКИ, ПОДДЕРЖИВАЮЩЕЙ ВЫГРУЗКУ ФАЙЛОВ В АРХИВНУЮ ПАПКУ НА СЕРВЕРЕ */
        /// <summary>
        /// Идентификатор режима открытия карточки "Открыть файлы"
        /// </summary>
        public Guid OpenFilesMode
        {
            get { return RefMarketingFilesCard.Modes.OpenFiles; }
        }
        /// <summary>
        /// Идентификатор режима открытия карточки "Открыть карточку и файлы"
        /// </summary>
        public Guid OpenCardAndFilesMode
        {
            get { return RefMarketingFilesCard.Modes.OpenCardAndFiles; }
        }

        /// <summary>
        /// Поле "Тип документа".
        /// </summary>
        CollectionControlView _Control_Categories;
        /// <summary>
        /// Поле "Файлы документа".
        /// </summary>
        TextEdit _Edit_Files;
        /// <summary>
        /// Кнопка "Загрузить файлы".
        /// </summary>
        SimpleButton _Button_Upload;
        /// <summary>
        /// Папка архива для временных папок.
        /// </summary>
        readonly String ArchiveTempPath;
        /// <summary>
        /// Поле "Вид оборудования".
        /// </summary>
        CollectionControlView Control_EquipmentSorts;
        /// <summary>
        /// Поле "Тип оборудования".
        /// </summary>
        CollectionControlView Control_EquipmentTypes;
        /// <summary>
        /// Поле "Производитель".
        /// </summary>
        CollectionControlView Control_Manufacturers;
        /// <summary>
        /// Ограничитель выбираемых типов обрудования.
        /// </summary>
        SelectHelper Selector_EquipmentTypes;
        #endregion

        /// <summary>
        /// Инициализирует карточку по заданным данным.
        /// </summary>
        /// <param name="ClassBase">Скрипт карточки.</param>
        public MarketingFilesCard(ScriptClassBase ClassBase)
            : base(ClassBase)
        {
            try
            {
                /* Назначение прав */
                NeedAssign = CardScript.CardControl.ActivateFlags.HasFlag(ActivateFlags.New) || CardScript.CardControl.ActivateFlags.HasFlag(ActivateFlags.NewFromTemplate);
                NeedSynchronize = false;

                Boolean Open = true;
                if (!NeedAssign)
                {
                    if (CardScript.CardControl.ModeId == RefMarketingFilesCard.Modes.OpenFiles || CardScript.CardControl.ModeId.IsEmpty())
                    {
                        if (MyHelper.OpenFolder(FolderPath).HasValue)
                        {
                            Open = false;
                            CardScript.CardFrame.Close();
                        }
                    }
                    else if (CardScript.CardControl.ModeId == RefMarketingFilesCard.Modes.OpenCardAndFiles)
                        MyHelper.OpenFolder(FolderPath);
                }

                if (Open)
                {
                    /* Получение рабочих объектов */
                    _Control_Categories = ICardControl.FindPropertyItem<CollectionControlView>(RefMarketingFilesCard.Categories.Alias);
                    _Button_Upload = ICardControl.FindPropertyItem<SimpleButton>(RefMarketingFilesCard.Buttons.Upload);
                    _Edit_Files = ICardControl.FindPropertyItem<TextEdit>(RefMarketingFilesCard.MainInfo.Files);
                    ArchiveTempPath = CardScript.Session.GetArchiveTempPath();
                    if (ICardControl.ContainsControl(RefMarketingFilesCard.EquipmentSorts.Alias))
                        Control_EquipmentSorts = ICardControl.FindPropertyItem<CollectionControlView>(RefMarketingFilesCard.EquipmentSorts.Alias);
                    if (ICardControl.ContainsControl(RefMarketingFilesCard.EquipmentTypes.Alias))
                        Control_EquipmentTypes = ICardControl.FindPropertyItem<CollectionControlView>(RefMarketingFilesCard.EquipmentTypes.Alias);
                    if (ICardControl.ContainsControl(RefMarketingFilesCard.Manufacturers.Alias))
                        Control_Manufacturers = ICardControl.FindPropertyItem<CollectionControlView>(RefMarketingFilesCard.Manufacturers.Alias);
                    if(!Control_EquipmentSorts.IsNull() && !Control_EquipmentTypes.IsNull())
                        Selector_EquipmentTypes = new SelectHelper(Context, SelectionType.BaseUniversalItem, Control_EquipmentTypes.TypeIds[0].NodeId, false, 
                            new List<String>() { Dynamic.CardBaseUniversalItem.EquipmentDirectory.ID + "\t" + Dynamic.CardBaseUniversalItem.EquipmentDirectory.Sort }, 
                            new List<String>() { Control_EquipmentSorts.SelectedItems.Select(item => item.ObjectId.ToUpper()).Aggregate("\t") }, false);

                    /* Привязка методов */
                    if (!IsReadOnly)
                    {
                        CardScript.CardControl.CardClosed -= CardControl_CardClosed;
                        CardScript.CardControl.CardClosed += CardControl_CardClosed;
                        CardScript.CardControl.Saved -= CardControl_Saved;
                        CardScript.CardControl.Saved += CardControl_Saved;
                        CardScript.CardControl.Saving -= CardControl_Saving;
                        CardScript.CardControl.Saving += CardControl_Saving;
                        _Button_Upload.Click -= Button_Upload_Click;
                        _Button_Upload.Click += Button_Upload_Click;
                        _Edit_Files.DoubleClick -= Edit_Files_DoubleClick;
                        _Edit_Files.DoubleClick += Edit_Files_DoubleClick;
                        if(!Control_EquipmentSorts.IsNull() && !Control_EquipmentTypes.IsNull())
                        {
                            Control_EquipmentSorts.ValueChanged -= Control_EquipmentSorts_ValueChanged;
                            Control_EquipmentSorts.ValueChanged += Control_EquipmentSorts_ValueChanged;
                            Control_EquipmentTypes.CustomChoosingValue -= Control_EquipmentTypes_CustomChoosingValue;
                            Control_EquipmentTypes.CustomChoosingValue += Control_EquipmentTypes_CustomChoosingValue;
                            Control_EquipmentTypes.CustomizeTypeSearchQuery -= Control_EquipmentTypes_CustomizeTypeSearchQuery;
                            Control_EquipmentTypes.CustomizeTypeSearchQuery += Control_EquipmentTypes_CustomizeTypeSearchQuery;
                        }
                    }

                    Customize();
                    RefreshChanges();
                }
            }
            catch (Exception Ex) { CallError(Ex); }
        }

        #region Methods
        /// <summary>
        /// Настройка внешнего вида.
        /// </summary>
        public override void Customize()
        {
            /* Настройка таблицы */
            ICardControl.HideTableBarItem(RefMarketingFilesCard.Properties.Alias, 2, true);
            if (!ICardControl.FindPropertyItem<IPropertyControl>(RefMarketingFilesCard.MainInfo.Folder).AllowEdit)
                ICardControl.FindLayoutItem(RefMarketingFilesCard.MainInfo.Folder).Visibility = LayoutVisibility.OnlyInCustomization;
        }
        /// <summary>
        /// Обновляет список изменений.
        /// </summary>
        public override void RefreshChanges()
        {
            Dictionary<String, ChangingValue<String>> changes = new Dictionary<String, ChangingValue<String>>();
            changes.Add(RefMarketingFilesCard.MainInfo.Name, new ChangingValue<String>((GetControlValue(RefMarketingFilesCard.MainInfo.Name) ?? String.Empty).ToString()));
            changes.Add(RefMarketingFilesCard.Categories.Alias, new ChangingValue<String>(_Control_Categories.SelectedItems.Any() ? Context.GetObject<CategoriesCategory>(_Control_Categories.SelectedItems[0].ObjectId).Name : String.Empty));
            if (!Changes.IsNull() && Changes.Keys.Contains(RefMarketingFilesCard.Categories.Alias) && Changes[RefMarketingFilesCard.Categories.Alias].IsChanged)
                NeedAssign = true;
            if (!Changes.IsNull())
                Changes.Clear();
            Changes = changes;
        }
        /// <summary>
        /// Выполняет проверку заполнености полей.
        /// </summary>
        /// <returns></returns>
        public Boolean Check()
        {
            Changes[RefMarketingFilesCard.MainInfo.Name].NewValue = (GetControlValue(RefMarketingFilesCard.MainInfo.Name) ?? String.Empty).ToString();
            Changes[RefMarketingFilesCard.Categories.Alias].NewValue = _Control_Categories.Text;

            List<String> Warns = new List<String>();
            List<String> EmptyFields = new List<String>();

            if (Changes[RefMarketingFilesCard.MainInfo.Name].NewValue.IsNull())
                EmptyFields.Add("Название документа");
            if (!_Control_Categories.SelectedItems.Any())
                EmptyFields.Add("Тип документа");
            if (!Control_EquipmentSorts.IsNull() && !Control_EquipmentSorts.SelectedItems.Any())
                EmptyFields.Add("Вид оборудования");
            if (!Control_EquipmentTypes.IsNull() && !Control_EquipmentTypes.SelectedItems.Any())
                EmptyFields.Add("Тип оборудования");
            if (!Control_Manufacturers.IsNull() && !Control_Manufacturers.SelectedItems.Any())
                EmptyFields.Add("Производитель");

            if (EmptyFields.Any())
                Warns.Add(EmptyFields.Count.GetCaseString("Не заполнено поле «" + EmptyFields[0] + "»!", "Не заполнены поля: " + Environment.NewLine + EmptyFields.Select(s => " - «" + s + "»").Aggregate(";" + Environment.NewLine) + "!"));

            if (Changes[RefMarketingFilesCard.MainInfo.Name].NewValue.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                Warns.Add("Название документа содержит недопустимые символы!");
            if ((_Control_Categories.SelectedItems.Any() ? Context.GetObject<CategoriesCategory>(_Control_Categories.SelectedItems[0].ObjectId).Name : String.Empty).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                Warns.Add("Тип документа содержит недопустимые символы!");

            if (Warns.Any())
            {
                MyMessageBox.Show(Warns.Aggregate(Environment.NewLine + Environment.NewLine), Warns.Count.GetCaseString("Предупреждение", "Предупреждения"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
        #endregion

        #region Event Handlers

        private new void Button_Upload_Click(Object sender, EventArgs e)
        {
            try
            {
                DirectoryInfo ArchiveTempFolder = new DirectoryInfo(ArchiveTempPath);
                if (Check())
                {
                    OpenFileDialog FileDialog = new OpenFileDialog();
                    FileDialog.CheckFileExists = true;
                    FileDialog.CheckPathExists = true;
                    FileDialog.Multiselect = true;
                    FileDialog.Title = "Выберите файлы для загрузки...";
                    FileDialog.ValidateNames = true;

                    switch (FileDialog.ShowDialog())
                    {
                        case DialogResult.OK:
                            if (FileDialog.FileNames.Length > 0)
                            {
                                String TempFolderName = Path.GetRandomFileName();
                                String TempFolderPath = Path.Combine(ArchiveTempPath, TempFolderName);
                                String Folder = FolderPath;

                                if (String.IsNullOrWhiteSpace(Folder))
                                {
                                    String Name = (GetControlValue(RefMarketingFilesCard.MainInfo.Name) ?? String.Empty).ToString();
                                    while (CardScript.Session.CheckDuplication(FolderName))
                                        Name += " 1";
                                    SetControlValue(RefMarketingFilesCard.MainInfo.Name, Name);
                                    Changes[RefMarketingFilesCard.MainInfo.Name].NewValue = Name;

                                    Folder = Path.Combine(CardScript.Session.GetArchivePath(), ArchiveSection, ArchiveSubSection, FolderName) + @"\";

                                    if (FileDialog.SafeFileNames.Any(file => (Path.Combine(Folder, file).Length + 1) > MyHelper.PathMaxLenght))
                                    {
                                        WriteLog("Длинный путь: " + Folder);
                                        throw new MyException(2, (MyHelper.PathMaxLenght - Folder.Length - 1).ToString());
                                    }

                                    ArchiveTempFolder.CreateSubdirectory(TempFolderName);
                                    foreach (String FileName in FileDialog.FileNames)
                                        File.Copy(FileName, Path.Combine(TempFolderPath, Path.GetFileName(FileName)));

                                    Folder = CardScript.Session.PlaceFiles(TempFolderPath, Folder);
                                    if (Folder != Boolean.FalseString)
                                    {
                                        SetControlValue(RefMarketingFilesCard.MainInfo.Files, FileDialog.SafeFileNames.OrderBy(s => s).Aggregate("; "));
                                        FolderPath = Folder;
                                    }
                                    else
                                        throw new MyException(1);

                                    FastSaving = true;
                                }
                                else
                                {
                                    if (FileDialog.SafeFileNames.Any(file => (Path.Combine(Folder, file).Length + 1) > MyHelper.PathMaxLenght))
                                    {
                                        WriteLog("Длинный путь: " + Folder);
                                        throw new MyException(2, (MyHelper.PathMaxLenght - Folder.Length - 1).ToString());
                                    }

                                    ArchiveTempFolder.CreateSubdirectory(TempFolderName);
                                    foreach (String FileName in FileDialog.FileNames)
                                        File.Copy(FileName, Path.Combine(TempFolderPath, Path.GetFileName(FileName)));

                                    if ((Folder = CardScript.Session.AddFiles(TempFolderName, Folder)) != Boolean.FalseString)
                                    {
                                        SetControlValue(RefMarketingFilesCard.MainInfo.Files, (GetControlValue(RefMarketingFilesCard.MainInfo.Files) ?? String.Empty).ToString().Split(';').Select(s => s.Trim())
                                            .ToList().Union(FileDialog.SafeFileNames).OrderBy(s => s).Aggregate("; "));
                                        FolderPath = Folder;
                                    }
                                    else
                                        throw new MyException(1);
                                }

                                CardScript.SaveCard();
                                FastSaving = false;
                            }
                            break;
                    }
                }
            }
            catch (MyException Ex)
            {
                switch (Ex.ErrorCode)
                {
                    case 1: MyMessageBox.Show("Не удалось загрузить файлы." + Environment.NewLine + "Обратитесь к администратору.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); break;
                    case 2: MyMessageBox.Show("Название файла не должно превышать " + Ex.Message + " символов.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Error); break;
                }
            }
            catch (Exception Ex) { CallError(Ex); }
        }

        private void CardControl_CardClosed (Object sender, EventArgs e)
        {
            try
            {
                if (NeedAssign)
                    CardScript.CardData.AssignRights();

                /* Отвязка методов */
                if (!Control_EquipmentTypes.IsNull())
                {
                    Control_EquipmentTypes.CustomizeTypeSearchQuery -= Control_EquipmentTypes_CustomizeTypeSearchQuery;
                    Control_EquipmentTypes.CustomChoosingValue -= Control_EquipmentTypes_CustomChoosingValue;
                }
                if (!Control_EquipmentSorts.IsNull())
                    Control_EquipmentSorts.ValueChanged -= Control_EquipmentSorts_ValueChanged;
                _Edit_Files.DoubleClick -= Edit_Files_DoubleClick;
                _Button_Upload.Click -= Button_Upload_Click;
                CardScript.CardControl.Saving -= CardControl_Saving;
                CardScript.CardControl.Saved -= CardControl_Saved;
                CardScript.CardControl.CardClosed -= CardControl_CardClosed;
            }
            catch (Exception Ex) { CallError(Ex); }
        }

        private void CardControl_Saved (Object sender, EventArgs e)
        {
            try
            {
                if (NeedSynchronize)
                {
                    if (CardScript.Session.Synchronize(CardScript.CardData.Id))
                    {
                        NeedSynchronize = false;
                        if (!String.IsNullOrWhiteSpace(FolderPath))
                        {
                            DirectoryInfo Folder = new DirectoryInfo(FolderPath);
                            if (!Folder.Parent.IsNull())
                            {
                                SetControlValue(RefMarketingFilesCard.MainInfo.Folder, Path.Combine(Folder.Parent.FullName, FolderName) + @"\");
                                FastSaving = true;
                                CardScript.SaveCard();
                                FastSaving = false;
                            }
                        }
                    }
                }
                StringBuilder Digest = new StringBuilder(FolderName);
                CardScript.UpdateDescription(CardScript.CardData.IsTemplate ? "Шаблон" : Digest.ToString());
            }
            catch (Exception Ex) { CallError(Ex); }
        }

        private void CardControl_Saving (Object sender, CancelEventArgs e)
        {
            try
            {
                if (Check())
                {
                    if (Changes[RefMarketingFilesCard.MainInfo.Name].IsChanged || Changes[RefMarketingFilesCard.Categories.Alias].IsChanged)
                    {
                        if (!FastSaving && CardScript.Session.CheckDuplication(ArchiveSection + "\t" + ArchiveSubSection + "\t" + FolderName))
                            throw new MyException(1);
                        String Folder = FolderPath;
                        if (!String.IsNullOrWhiteSpace(Folder))
                        {
                            DirectoryInfo FolderInfo = new DirectoryInfo(Folder);
                            if (FolderInfo.Exists)
                            {
                                List<String> Files = FolderInfo.GetFiles().Select(file => file.Name).ToList();
                                SetControlValue(RefMarketingFilesCard.MainInfo.Files, Files.Aggregate("; "));
                                String NewFolder = Path.Combine(CardScript.Session.GetArchivePath(), ArchiveSection, ArchiveSubSection, FolderName);
                                if (!FastSaving && Files.Any(file => (Path.Combine(NewFolder, file).Length + 1) > MyHelper.PathMaxLenght))
                                    throw new MyException(2);
                            }
                            NeedSynchronize = true;
                        }
                    }
                }
                else
                    e.Cancel = true;
                RefreshChanges();
            }
            catch (MyException Ex)
            {
                switch (Ex.ErrorCode)
                {
                    case 1: MyMessageBox.Show("Документ с таким названием и типом уже существует!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning); break;
                    case 2: MyMessageBox.Show("Невозможно перенести файлы!" + Environment.NewLine + "Названия файлов превышают допустимую длину.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning); break;
                }
                e.Cancel = true;
            }
            catch (Exception Ex)
            {
                CallError(Ex);
                e.Cancel = true;
            }
        }

        private void Control_EquipmentSorts_ValueChanged(Object sender, EventArgs e)
        {
            Selector_EquipmentTypes.PropertyValues = new List<String>() { Control_EquipmentSorts.SelectedItems.Select(item => item.ObjectId.ToUpper()).Aggregate("\t") };
            Control_EquipmentTypes.ChangeItems(Selector_EquipmentTypes.GetItems().Select(item => item.Id).Intersect(Control_EquipmentTypes.SelectedItems.Select(item => item.ObjectId)).ToList());
        }

        private void Control_EquipmentTypes_CustomChoosingValue(Object sender, CustomChoosingValueEventArgs e)
        {
            e.Handled = true;
            MultySelectForm Form = new MultySelectForm("Тип оборудования:",
                CardScript.CardFrame.CardHost, Context, Selector_EquipmentTypes.Type, Selector_EquipmentTypes.Constraint,
                Control_EquipmentTypes.SelectedItems.Select(item => Selector_EquipmentTypes.GetItem(item.ObjectId)).ToList(), false,
                Selector_EquipmentTypes.AllowSubConstraint, false,
                Selector_EquipmentTypes.PropertyValues[0].IsNull() ? null : Selector_EquipmentTypes.PropertyNames,
                Selector_EquipmentTypes.PropertyValues[0].IsNull() ? null : Selector_EquipmentTypes.PropertyValues,
                Selector_EquipmentTypes.AllowNull);
            Form.ShowDialog();
            Control_EquipmentTypes.ChangeItems(Form.SelectedItems.Select(item => item.Id).ToList());
        }

        private void Control_EquipmentTypes_CustomizeTypeSearchQuery(Object sender, CustomizeTypeSearchQueryEventArgs e)
        {
            e.SectionQuery.JoinSections.Clear();
            for (Int32 i = 0; i < e.SectionQuery.ConditionGroup.ConditionGroups.Count; i++)
                if (!e.SectionQuery.ConditionGroup.ConditionGroups[i].Conditions[0].SectionAlias.IsNull())
                    e.SectionQuery.ConditionGroup.ConditionGroups.Remove(i--);

            if (!Selector_EquipmentTypes.PropertyValues.IsNull() && Selector_EquipmentTypes.PropertyValues.Count > 0 && !Selector_EquipmentTypes.PropertyValues[0].IsNull())
            {
                String JoinSectionCardItemName = CardBaseUniversalItem.Alias + "_1";
                JoinSection JoinSectionProperties = e.SectionQuery.JoinSections.AddNew(JoinSectionCardItemName);
                JoinSectionProperties.Id = Dynamic.CardBaseUniversalItem.EquipmentDirectory.ID;
                JoinSectionProperties.SectionField = "InstanceID";
                JoinSectionProperties.WithField = RefBaseUniversal.Items.ItemCard;

                ConditionGroup SubConditionGroup = e.SectionQuery.ConditionGroup.ConditionGroups.AddNew();
                SubConditionGroup.Operation = ConditionGroupOperation.And;
                SubConditionGroup.Conditions.AddNew(Dynamic.CardBaseUniversalItem.EquipmentDirectory.Sort, FieldType.UniqueId, ConditionOperation.OneOf, Selector_EquipmentTypes.PropertyValues[0].Split('\t')).SectionAlias = JoinSectionCardItemName;
            }
        }

        //private void Edit_Files_DoubleClick (Object sender, EventArgs e)
        //{
        //    try
        //    {
        //        String FolderPath = (GetControlValue(RefMarketingFilesCard.MainInfo.Folder) ?? String.Empty).ToString();
        //        Boolean? Open = OpenFolder(FolderPath);
        //        if(Open.HasValue && !Open.Value)
        //            MyMessageBox.Show("Папка " + FolderPath + " не существует!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }
        //    catch (Exception Ex) { CallError(Ex); }
        //}

        #endregion

        
    }
}
