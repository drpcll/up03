using System.Windows.Controls;
using AuctionSystem.Models;
using AuctionSystem.Services;

namespace AuctionSystem.Views;

public partial class ParticipantsView : UserControl
{
    public ParticipantsView()
    {
        InitializeComponent();

        var isAdmin = Session.IsAdministrator || Session.IsAuctionAdministrator;

        if (isAdmin || Session.IsSeller)
        {
            Tabs.Items.Add(new TabItem
            {
                Header = "Продавцы",
                Content = new SectionView(SectionRegistry.Sellers())
            });
        }

        if (isAdmin || Session.IsBuyer)
        {
            Tabs.Items.Add(new TabItem
            {
                Header = "Покупатели",
                Content = new SectionView(SectionRegistry.Buyers())
            });
        }
    }
}
