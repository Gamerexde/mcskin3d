﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using BrightIdeasSoftware;
using System.Xml;
using System.IO;
using System.Threading;
using System.Net;
using System.Windows.Forms.VisualStyles;
using System.Collections;

namespace MCSkin3D.UpdateSystem
{
	public partial class Updater : Form
	{
		public string UpdateXMLURL { get; set; }

		public Updater()
		{
			InitializeComponent();
		}

		public Updater(string url) :
			this()
		{
			UpdateXMLURL = url;
		}

		private void listView1_SelectedIndexChanged(object sender, EventArgs e)
		{
		}

		class UpdateItem : IComparable<UpdateItem>
		{
			public string Name { get; set; }
			public string Information { get; set; }
			public string Size { get; set; }
			public string Date { get; set; }
			public string Group { get; set; }
			public bool IsChecked { get; set; }
			public string DownloadURL { get; set; }
			public string GroupImageURL { get; set; }
			public int Progress { get; set; }
			public DateTime RealDate { get; set; }

			public int ImageIndex { get; set; }

			public UpdateItem() { }

			public int CompareTo(UpdateItem item)
			{
				return RealDate.CompareTo(item.RealDate);
			}
		}

		// FIXME: RSS instead?
		static List<UpdateItem> LoadUpdates(XmlDocument document)
		{
			List<UpdateItem> items = new List<UpdateItem>();

			if (document.DocumentElement.Name.ToLower() == "updates")
			{
				foreach (XmlNode x in document.DocumentElement.ChildNodes)
				{
					if (x.Name.ToLower() != "update")
						continue;

					UpdateItem item = new UpdateItem();

					foreach (XmlNode subNode in x.ChildNodes)
					{
						if (subNode.Name.ToLower() == "name")
							item.Name = subNode.InnerText;
						else if (subNode.Name.ToLower() == "info")
							item.Information = subNode.InnerText;
						else if (subNode.Name.ToLower() == "size")
							item.Size = subNode.InnerText;
						else if (subNode.Name.ToLower() == "date")
						{
							item.Date = subNode.InnerText;

							string[] data = item.Date.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
							int month = int.Parse(data[0]);
							int day = int.Parse(data[1]);
							int year = int.Parse(data[2]);

							item.RealDate = new DateTime(year, month, day);
						}
						else if (subNode.Name.ToLower() == "group")
							item.Group = subNode.InnerText;
						else if (subNode.Name.ToLower() == "ismajor")
							item.IsChecked = bool.Parse(subNode.InnerText);
						else if (subNode.Name.ToLower() == "downloadurl")
							item.DownloadURL = subNode.InnerText;
						else if (subNode.Name.ToLower() == "groupimageurl")
							item.GroupImageURL = subNode.InnerText;
					}

					items.Add(item);
				}
			}

			return items;
		}

		List<UpdateItem> _updates = null;
		Thread _updateThread = null;
		static ImageList _updateImages = new ImageList();
		static readonly string _tempDirectory = Environment.ExpandEnvironmentVariables("%temp%");
		static readonly string _mcskin3dTemp = _tempDirectory + '\\' + "mcskin3d";

		void GetUpdateData()
		{
			WebClient client = new WebClient();
			XmlDocument doc = new XmlDocument();

			_updateImages.ColorDepth = ColorDepth.Depth32Bit;

			using (MemoryStream ms = new MemoryStream())
			{
				var data = client.DownloadData(UpdateXMLURL);
				ms.Write(data, 0, data.Length);

				ms.Position = 0;

				using (var sr = new StreamReader(ms, Encoding.Unicode))
					doc.LoadXml(sr.ReadToEnd());
			}

			_updates = LoadUpdates(doc);
			_updates.Sort();

			List<string> fileNames = new List<string>();

			foreach (var u in _updates)
			{
				string url = u.GroupImageURL;
				string fileName = Path.GetFileName(url);

				if (!Directory.Exists(_mcskin3dTemp))
					Directory.CreateDirectory(_mcskin3dTemp);

				var fileDir = _mcskin3dTemp + '\\' + fileName;

				if (!File.Exists(fileDir))
					client.DownloadFile(url, fileDir);

				if (!fileNames.Contains(fileDir))
				{
					fileNames.Add(fileDir);
					_updateImages.Images.Add(Image.FromFile(fileDir));
				}

				u.ImageIndex = fileNames.IndexOf(fileDir);
			}

			Invoke((Action)delegate() { UpdateFinished(); });
		}

		void UpdateFinished()
		{
			objectListView1.SmallImageList = objectListView1.LargeImageList = _updateImages;

			objectListView1.BeginUpdate();
			objectListView1.SetObjects(_updates);
			objectListView1.Columns[1].Width = objectListView1.Width - (objectListView1.Columns[0].Width + objectListView1.Columns[2].Width + objectListView1.Columns[3].Width) - 4;
			objectListView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
			objectListView1.EndUpdate();
			objectListView1.Sort(3);

			objectListView1.Enabled = true;
			button1.Enabled = true;
			panel2.Visible = false;
		}

		static readonly object lockObj = new object();

		void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
			lock (lockObj)
				Monitor.Pulse(lockObj);
		}

		void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			Invoke((Action)delegate()
			{
				((UpdateItem)e.UserState).Progress = e.ProgressPercentage;
				objectListView1.RedrawItems(0, _updates.Count - 1, false);
			});
		}

		void DownloadUpdateData()
		{
			WebClient client = new WebClient();

			client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
			client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);

			foreach (var u in _updates)
			{
				if (!objectListView1.IsChecked(u))
					continue;

				string url = u.DownloadURL;

				string fileName = Path.GetFileName(url);

				if (!Directory.Exists(_mcskin3dTemp))
					Directory.CreateDirectory(_mcskin3dTemp);

				var fileDir = _mcskin3dTemp + '\\' + fileName;

				client.DownloadFileAsync(new Uri(url), fileDir, u);

				lock (lockObj)
					Monitor.Wait(lockObj);
			}

			Invoke((Action)delegate()
			{
				DialogResult = DialogResult.OK;
				Close();
			});
		}

		private void button1_Click(object sender, EventArgs e)
		{
			objectListView1.BeginUpdate();
			olvColumn2.IsVisible = false;
			olvColumn5.IsVisible = true;
			objectListView1.RebuildColumns();
			objectListView1.EndUpdate();

			_updateThread = new Thread(DownloadUpdateData);
			_updateThread.Start();
		}

		private void Updater_Load(object sender, EventArgs e)
		{
			_updateThread = new Thread(GetUpdateData);
			_updateThread.Start();

			objectListView1.BeginUpdate();
			objectListView1.View = View.Details;
			objectListView1.CheckBoxes = true;
			objectListView1.GridLines = true;
			objectListView1.AllowColumnReorder = false;
			objectListView1.FullRowSelect = true;
			objectListView1.ShowItemCountOnGroups = true;
			objectListView1.OwnerDraw = true;

			olvColumn5.Renderer = new BarRenderer(0, 100);

			foreach (var c in objectListView1.Columns)
			{
				OLVColumn col = (OLVColumn)c;

				col.GroupKeyGetter = delegate(object row)
				{
					return ((UpdateItem)row).Group;
				};

				col.GroupKeyToTitleConverter = delegate(object key)
				{
					return (string)key;
				};
			}

			objectListView1.AllColumns[0].RendererDelegate = delegate(EventArgs args, Graphics g, Rectangle r, Object rowObject)
			{
				g.FillRectangle(new SolidBrush(objectListView1.BackColor), new Rectangle(r.X - 1, r.Y - 1, objectListView1.Width, r.Height + 2));

				DrawListViewSubItemEventArgs realArgs = (DrawListViewSubItemEventArgs)args;

				/*if ((realArgs.ItemState & ListViewItemStates.Selected) != 0)
				{
					using (var brush = new SolidBrush(SystemColors.Highlight))
						g.FillRectangle(brush, r.X - 1, r.Y, r.Width + 2, r.Height);
				}*/

				bool isHot = objectListView1.HotRowIndex == realArgs.ItemIndex && objectListView1.PointToClient(Cursor.Position).X < r.X + 5 + 17;

				CheckBoxRenderer.DrawCheckBox(g, new Point(r.X + 5, r.Y + 2), realArgs.Item.Checked ? (isHot ? CheckBoxState.CheckedHot : CheckBoxState.CheckedNormal) : (isHot ? CheckBoxState.UncheckedHot : CheckBoxState.UncheckedNormal));

				g.DrawImage(realArgs.Item.ImageList.Images[((UpdateItem)rowObject).ImageIndex], new Point(r.X + 24, r.Y));

				TextRenderer.DrawText(g, realArgs.Item.Text, Font, new Rectangle(r.X + 24 + 16, r.Y, r.Width - (24 + 16), r.Height), SystemColors.ControlText,
					TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

				return true;
			};

			objectListView1.CheckedAspectName = "IsChecked";
			objectListView1.EndUpdate();
		}


		void objectListView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
		}

		void objectListView1_DrawItem(object sender, DrawListViewItemEventArgs e)
		{
		}

		private void button2_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}