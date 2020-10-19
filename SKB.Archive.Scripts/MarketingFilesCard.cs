using System;
using System.Windows.Forms;
using DocsVision.BackOffice.WinForms;
using DocsVision.BackOffice.ObjectModel;
using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectModel;
using DocsVision.Platform.WinForms;
using SKB.Archive.Cards;

namespace Archive
{
    public class MarketingFilesCardScript : ScriptClassBase
    {

        #region Properties

        MarketingFilesCard Card;

        #endregion

        #region Methods

        #endregion

        #region Event Handlers

        private void MarketingFilesCard_CardActivated (Object sender, CardActivatedEventArgs e)
        {
            Card = new MarketingFilesCard(this);
        }

        #endregion

    }
}