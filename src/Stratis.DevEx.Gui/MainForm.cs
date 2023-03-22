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
            projectViews.Add(new WebView());
            projectViews[0].LoadHtml(@"<html>
<head><title>Hello!</title></head>
<body>
	<h1>Some custom html</h1>
	<script type='text/javascript'>
		var titleChange = 0;

		function appendResult(id, value) {
			var element = document.getElementById(id);

			var result = document.createElement('li');
			result.appendChild(document.createTextNode('result: ' + value));
			element.appendChild(result);
		}
	</script>
	<form method='post' enctype='multipart/form-data'>
		<p><h2>Test Printing</h2>
			<button onclick='print(); return false;'>Print</button>
		</p>
		<p><h2>Test Selecting a File</h2>
			<input type='file'>
		</p>
		<p><h2>Test Alert</h2>
			<button onclick='alert(""This is an alert""); return false;'>Show Alert</button>
		</p>
		<p><h2>Test Confirm</h2>
			<button onclick=""appendResult('confirmResult', confirm('Confirm yes or no')); return false;"">Show Confirm</button>
			<ul id='confirmResult'></ul>
		</p>
		<p><h2>Test Prompt</h2>
			<button onclick=""appendResult('inputResult', prompt('Enter some text', 'some default text')); return false;"">Show Prompt</button>
			<ul id='inputResult'></ul>
		</p>
		<p><h2>Test Navigation</h2>
			<button onclick=""window.location = 'http://www.example.com'; return false;"">Set location</button>
			<button onclick=""window.open('http://www.example.com'); return false;"">Open new window</button>
			<button onclick=""window.open('http://www.example.com', 'name_of_new_window'); return false;"">Open named window</button>
			<br>
			<a href='http://www.example.com'>Open link in this window</a>
			<a href='http://www.example.com' target='_blank'>Open in new window</a>
			<a href='http://www.example.com' target='another_name_of_new_window'>Open in named window</a>
		</p>
		<p><h2>Test Custom Protocol</h2>
			<button onclick=""window.location='eto:dosomething'; return false;"">window.location='eto:dosomething'</button>
			<button onclick=""window.location.replace('eto://dosomething')"">window.location.replace('eto://dosomething')</button>
			<button onclick=""window.open('eto:dosomething'); return false;"">window.open('eto:dosomething')</button>
			<button onclick=""window.open('eto:dosomething', 'name_of_new_window'); return false;"">window.open('eto:dosomething', 'name_of_new_window')</button>
			<br>
			<a href='eto:dosomething'>href='eto:dosomething'</a>
			<a href='eto:dosomething' target='_blank'>href='eto:dosomething' target='_blank'</a>
			<a href='eto:dosomething' target='another_name_of_new_window'>href='eto:dosomething' target='another_name_of_new_window'</a>
		</p>
		<p><h2>Dynamic title changes</h2>
			<button onclick=""document.title = 'some title ' + titleChange++; return false;"">Change title</button>
		</p>
		<h2>Input Types</h2>
		<table>
			<tr>
				<td>Button</td>
				<td><input type='button'></td>
			</tr>
			<tr>
				<td>Checkbox</td>
				<td><input type='checkbox'></td>
			</tr>
			<tr>
				<td>Color</td>
				<td><input type='color'></td>
			</tr>
			<tr>
				<td>Date</td>
				<td><input type='date'></td>
			</tr>
			<tr>
				<td>DateTime</td>
				<td><input type='datetime'></td>
			</tr>
			<tr>
				<td>Email</td>
				<td><input type='email'></td>
			</tr>
			<tr>
				<td>File</td>
				<td><input type='file'></td>
			</tr>
			<tr>
				<td>Hidden</td>
				<td><input type='hidden'></td>
			</tr>
			<tr>
				<td>Image</td>
				<td><input type='image'></td>
			</tr>
			<tr>
				<td>Month</td>
				<td><input type='month'></td>
			</tr>
			<tr>
				<td>Number</td>
				<td><input type='number'></td>
			</tr>
			<tr>
				<td>Password</td>
				<td><input type='password'></td>
			</tr>
			<tr>
				<td>Radio</td>
				<td><input type='radio'></td>
			</tr>
			<tr>
				<td>Range</td>
				<td><input type='range'></td>
			</tr>
			<tr>
				<td>Reset</td>
				<td><input type='reset'></td>
			</tr>
			<tr>
				<td>Search</td>
				<td><input type='search'></td>
			</tr>
			<tr>
				<td>Submit</td>
				<td><input type='submit'></td>
			</tr>
			<tr>
				<td>Tel</td>
				<td><input type='tel'></td>
			</tr>
			<tr>
				<td>Text</td>
				<td><input type='text'></td>
			</tr>
			<tr>
				<td>Time</td>
				<td><input type='time'></td>
			</tr>
			<tr>
				<td>Url</td>
				<td><input type='url'></td>
			</tr>
			<tr>
				<td>Week</td>
				<td><input type='week'></td>
			</tr>
			<tr>
				<td>TextArea</td>
				<td><textarea></textarea></td>
			</tr>
		</table>
	</form>
</body>

</html>");
            splitter = new Splitter();
            splitter.Panel1 = navigation;
			splitter.Panel2 = projectViews[0];
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
                Image = TestIcon
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
		protected static Icon TestIcon => Icon.FromResource("Stratis.DevEx.Gui.Images.TestIcon.ico");
		#pragma warning disable CS0618 // Type or member is obsolete
		protected TreeView navigation;
		#pragma warning restore CS0618 // Type or member is obsolete
		
		protected List<WebView> projectViews;
        protected Splitter splitter;
        #endregion
    }
}
