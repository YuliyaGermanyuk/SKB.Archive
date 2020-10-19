using DocsVision.BackOffice.ObjectModel;
using DocsVision.BackOffice.WinForms;
using DocsVision.Platform.CardHost;
using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.WinForms;
using SKB.Base;
using SKB.Base.Ref;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SKB.Base.Controls
{
    /// <summary>
    /// Компонент карточки "Файлы Маркетинга".
    /// </summary>
    [Customizable(true)]
    [CardFrameWindowType(typeof(CardFrameForm))]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    [Guid("65501459-7F9C-4B7A-822A-45E49C80FFE6")]
    public sealed class MarketingFilesCardControl : BaseCardControl
    {
        /// <summary>
        /// Инициализирует компонент карточки.
        /// </summary>
        public MarketingFilesCardControl (): base() { }
        /// <summary>
        /// Обработчик события, инициируемого после активации компонента карточки.
        /// </summary>
        /// <param name="e">Параметры активации.</param>
        protected override void OnCardActivated (CardActivatedEventArgs e)
        {
            base.OnCardActivated(e);
        }
        /// <summary>
        /// Обработчик специального события, инициируемого после активации пользоваетелем одного из методов карточки.
        /// </summary>
        /// <param name="e">Параметры метода карточки.</param>
        protected override void OnCardAction (CardActionEventArgs e)
        {
            base.OnCardAction(e);
            try
            {
                if (e.ActionId == RefMarketingFilesCard.Actions.OpenFiles)
                {
                    if (!CardData.IsNull())
                    {
                        String FolderPath = CardData.Sections[RefMarketingFilesCard.MainInfo.ID].FirstRow.GetString(RefMarketingFilesCard.MainInfo.Folder);
                        if (!String.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath))
                            Process.Start("explorer", "\"" + FolderPath + "\"");
                        else
                            CardHost.ShowCard(CardData.Id, RefMarketingFilesCard.Modes.OpenFiles, this.CardData.ArchiveState == ArchiveState.NotArchived ? ActivateMode.Edit : ActivateMode.ReadOnly);
                    }
                }
                else if (e.ActionId == RefMarketingFilesCard.Actions.OpenCardAndFiles)
                    CardHost.ShowCard(CardData.Id, RefMarketingFilesCard.Modes.OpenCardAndFiles, this.CardData.ArchiveState == ArchiveState.NotArchived ? ActivateMode.Edit : ActivateMode.ReadOnly);
                else if (e.ActionId == RefMarketingFilesCard.Actions.OpenCard)
                    CardHost.ShowCard(CardData.Id, RefMarketingFilesCard.Modes.OpenCard, this.CardData.ArchiveState == ArchiveState.NotArchived ? ActivateMode.Edit : ActivateMode.ReadOnly);
                else if (e.ActionId == RefMarketingFilesCard.Actions.Delete)
                {
                    IList<StatesOperation> Operations = StateService.GetOperations(BaseObject.SystemInfo.CardKind) ?? new List<StatesOperation>();
                    StatesOperation Operation = Operations.FirstOrDefault(item => item.DefaultName == "Modify");
                    if (!Operation.IsNull())
                    {
                        if (AccessCheckingService.IsOperationAllowed(BaseObject, Operation))
                        {
                            switch (ShowMessage("Вы уверены, что хотите удалить выбранную карточку и связанные файлы?", "Docsvision Navigator", MessageType.Question, MessageButtons.YesNo))
                            {
                                case MessageResult.Yes:
                                    Boolean ByMe;
                                    String OwnerName;
                                    if (!LockService.IsObjectLocked<BaseCard>(BaseObject, out ByMe, out OwnerName))
                                    {
                                        if (Session.DeleteCard(CardData.Id))
                                            ShowMessage("Карточка и файлы удалены!", "Docsvision Navigator", MessageType.Information, MessageButtons.Ok);
                                        else
                                            ShowMessage("Не удалось удалить карточку!" + Environment.NewLine 
                                                + "Обратитесь к системному администратору!", "Docsvision Navigator", MessageType.Error, MessageButtons.Ok);
                                    }
                                    else
                                        ShowMessage("Невозможно удалить карточку " + BaseObject.Description + "." + Environment.NewLine
                                                + "Карточка заблокирована " + (ByMe ? "вами" : "пользователем " + OwnerName) + "!", "Docsvision Navigator", MessageType.Warning, MessageButtons.Ok);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {this.ProcessException(ex);}
        }
    }
}