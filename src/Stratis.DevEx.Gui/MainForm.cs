using System;
using System.Collections.Generic;

using Eto.Drawing;
using Eto.Forms;


namespace Stratis.DevEx.Gui
{
    public partial class MainForm : Form
    {
        #region Constructors
        public MainForm()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            navigation = new TreeView()
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Size = new Size(100, 150)
            };
            navigation.DataStore = CreateTreeItem(0, "Item");
            projectViews = new List<WebView>();
            splitter = new Splitter();
            splitter.Panel1 = navigation;
            // splitter.Panel2
            Title = "Stratis DevEx";
            MinimumSize = new Size(200, 200);
            Content = splitter;
            /*
            Content = new StackLayout
            {
                Padding = 10,
                Items =
                {
                    "Hello World!",
					// add more controls here
				}
            };
            */
            // create a few commands that can be used for the menu and toolbar
            var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
            clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

            var quitCommand = new Command { MenuText = "Quit", Shortcut = GuiApp.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => GuiApp.Instance.Quit();

            var aboutCommand = new Command { MenuText = "About..." };
            aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

            // create menu
            Menu = new MenuBar
            {
                Items =
                {
					// File submenu
					new SubMenuItem { Text = "&File", Items = { clickMe } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
                ApplicationItems =
                {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
                },
                QuitItem = quitCommand,
                AboutItem = aboutCommand
            };

            // create toolbar			
            ToolBar = new ToolBar { Items = { clickMe } };
        }
        #endregion

        #region Methods
        TreeItem CreateTreeItem(int level, string name)
        {
            var item = new TreeItem
            {
                Text = name,
                //Expanded = expanded++ % 2 == 0,
                //Image = image
            };
            if (level < 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    item.Children.Add(CreateTreeItem(level + 1, name + " " + i));
                }
            }
            return item;
        }
        #endregion

        #region Fields
#pragma warning disable CS0618 // Type or member is obsolete
        protected TreeView navigation;
#pragma warning restore CS0618 // Type or member is obsolete
        protected List<WebView> projectViews;
        protected Splitter splitter;
        #endregion
    }
}
