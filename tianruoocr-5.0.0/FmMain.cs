﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using CsharpHttpHelper;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MSScriptControl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShareX.ScreenCaptureLib;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace TrOCR
{

	public partial class FmMain : Form
	{

		public FmMain()
		{
			set_merge = false;
			set_split = false;
			set_split = false;
			StaticValue.截图排斥 = false;
			pinyin_flag = false;
			tranclick = false;
			are = new AutoResetEvent(false);
			imagelist = new List<Image>();
			StaticValue.v_notecount = Convert.ToInt32(IniHelp.GetValue("配置", "记录数目"));
			baidu_flags = "";
			esc = "";
			voice_count = 0;
			fmnote = new Fmnote();
			fmflags = new FmFlags();
			pubnote = new string[StaticValue.v_notecount];
			for (var i = 0; i < StaticValue.v_notecount; i++)
			{
				pubnote[i] = "";
			}
			StaticValue.v_note = pubnote;
			StaticValue.mainhandle = Handle;
			Font = new Font(Font.Name, 9f / StaticValue.Dpifactor, Font.Style, Font.Unit, Font.GdiCharSet, Font.GdiVerticalFont);
			googleTranslate_txt = "";
			num_ok = 0;
			F_factor = Program.factor;
			components = null;
			InitializeComponent();
			nextClipboardViewer = (IntPtr)HelpWin32.SetClipboardViewer((int)Handle);
			InitMinimize();
			readIniFile();
			WindowState = FormWindowState.Minimized;
			Visible = false;
			split_txt = "";
			MinimumSize = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
			speak_copy = false;
			OCR_foreach("");
		}

		private void Load_Click(object sender, EventArgs e)
		{
			WindowState = FormWindowState.Minimized;
			Visible = false;
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 953)
			{
				speaking = false;
			}
			if (m.Msg == 274 && (int)m.WParam == 61536)
			{
				WindowState = FormWindowState.Minimized;
				Visible = false;
				return;
			}
			if (m.Msg == 600 && (int)m.WParam == 725)
			{
				if (IniHelp.GetValue("工具栏", "顶置") == "True")
				{
					TopMost = true;
					return;
				}
				TopMost = false;
				return;
			}
			else
			{
				if (m.Msg == 786 && m.WParam.ToInt32() == 530 && RichBoxBody.Text != null)
				{
					p_note(RichBoxBody.Text);
					StaticValue.v_note = pubnote;
					if (fmnote.Created)
					{
						fmnote.Text_note = "";
					}
				}
				if (m.Msg == 786 && m.WParam.ToInt32() == 520)
				{
					fmnote.Show();
					fmnote.Focus();
					fmnote.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - fmnote.Width, Screen.PrimaryScreen.WorkingArea.Height - fmnote.Height);
					fmnote.WindowState = FormWindowState.Normal;
					return;
				}
				if (m.Msg == 786 && m.WParam.ToInt32() == 580)
				{
					HelpWin32.UnregisterHotKey(Handle, 205);
					change_QQ_screenshot = false;
					FormBorderStyle = FormBorderStyle.None;
					Hide();
					if (transtalate_fla == "开启")
					{
						form_width = Width / 2;
					}
					else
					{
						form_width = Width;
					}
					form_height = Height;
					minico.Visible = false;
					minico.Visible = true;
					menu.Close();
					menu_copy.Close();
					auto_fla = "开启";
					split_txt = "";
					RichBoxBody.Text = "***该区域未发现文本***";
					RichBoxBody_T.Text = "";
					typeset_txt = "";
					transtalate_fla = "关闭";
					Trans_close.PerformClick();
					Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
					FormBorderStyle = FormBorderStyle.Sizable;
					StaticValue.截图排斥 = true;
					image_screen = StaticValue.image_OCR;
					if (IniHelp.GetValue("工具栏", "分栏") == "True")
					{
						minico.Visible = true;
						thread = new Thread(ShowLoading);
						thread.Start();
						ts = new TimeSpan(DateTime.Now.Ticks);
						var image = image_screen;
						var image2 = new Bitmap(image.Width, image.Height);
						var graphics = Graphics.FromImage(image2);
						graphics.DrawImage(image, 0, 0, image.Width, image.Height);
						graphics.Save();
						graphics.Dispose();
						image_ori = image2;
						((Bitmap)FindBundingBox_fences((Bitmap)image)).Save("Data\\分栏预览图.jpg");
					}
					else
					{
						minico.Visible = true;
						thread = new Thread(ShowLoading);
						thread.Start();
						ts = new TimeSpan(DateTime.Now.Ticks);
						var messageload = new Messageload();
						messageload.ShowDialog();
						if (messageload.DialogResult == DialogResult.OK)
						{
							esc_thread = new Thread(Main_OCR_Thread);
							esc_thread.Start();
						}
					}
				}
				if (m.Msg == 786 && m.WParam.ToInt32() == 590 && speak_copyb == "朗读")
				{
					TTS();
					return;
				}
				if (m.Msg == 786 && m.WParam.ToInt32() == 511)
				{
					base.MinimumSize = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
					transtalate_fla = "关闭";
					RichBoxBody.Dock = DockStyle.Fill;
					RichBoxBody_T.Visible = false;
					PictureBox1.Visible = false;
					RichBoxBody_T.Text = "";
					if (WindowState == FormWindowState.Maximized)
					{
						WindowState = FormWindowState.Normal;
					}
					Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
				}
				if (m.Msg == 786 && m.WParam.ToInt32() == 512)
				{
					transtalate_Click();
				}
				if (m.Msg == 786 && m.WParam.ToInt32() == 518)
				{
					if (ActiveControl.Name == "htmlTextBoxBody")
					{
						htmltxt = RichBoxBody.Text;
					}
					if (ActiveControl.Name == "rich_trans")
					{
						htmltxt = RichBoxBody_T.Text;
					}
					if (htmltxt == "")
					{
						return;
					}
					TTS();
				}
				if (m.Msg == 161)
				{
					HelpWin32.SetForegroundWindow(Handle);
					base.WndProc(ref m);
					return;
				}
				if (m.Msg != 163)
				{
					if (m.Msg == 786 && m.WParam.ToInt32() == 222)
					{
						try
						{
							StaticValue.截图排斥 = false;
							esc = "退出";
							fmloading.fml_close = "窗体已关闭";
							esc_thread.Abort();
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message);
						}
						FormBorderStyle = FormBorderStyle.Sizable;
						Visible = true;
						base.Show();
						WindowState = FormWindowState.Normal;
						if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
						{
							var value = IniHelp.GetValue("快捷键", "翻译文本");
							var text = "None";
							var text2 = "F9";
							SetHotkey(text, text2, value, 205);
						}
						HelpWin32.UnregisterHotKey(Handle, 222);
					}
					if (m.Msg == 786 && m.WParam.ToInt32() == 200)
					{
						HelpWin32.UnregisterHotKey(Handle, 205);
						menu.Hide();
						RichBoxBody.Hide = "";
						RichBoxBody_T.Hide = "";
						Main_OCR_Quickscreenshots();
					}
					if (m.Msg == 786 && m.WParam.ToInt32() == 206)
					{
						if (!fmnote.Visible || base.Focused)
						{
							fmnote.Show();
							fmnote.WindowState = FormWindowState.Normal;
							fmnote.Visible = true;
						}
						else
						{
							fmnote.Hide();
							fmnote.WindowState = FormWindowState.Minimized;
							fmnote.Visible = false;
						}
					}
					if (m.Msg == 786 && m.WParam.ToInt32() == 235)
					{
						if (!Visible)
						{
							TopMost = true;
							base.Show();
							WindowState = FormWindowState.Normal;
							Visible = true;
							Thread.Sleep(100);
							if (IniHelp.GetValue("工具栏", "顶置") == "False")
							{
								TopMost = false;
								return;
							}
						}
						else
						{
							Hide();
							Visible = false;
						}
					}
					if (m.Msg == 786 && m.WParam.ToInt32() == 205)
					{
						翻译文本();
					}
					base.WndProc(ref m);
					return;
				}
				if (transtalate_fla == "开启")
				{
					WindowState = FormWindowState.Normal;
					Size = new Size((int)font_base.Width * 23 * 2, (int)font_base.Height * 24);
					Location = (Point)new Size(Screen.PrimaryScreen.Bounds.Width / 2 - Screen.PrimaryScreen.Bounds.Width / 10 * 2, Screen.PrimaryScreen.Bounds.Height / 2 - Screen.PrimaryScreen.Bounds.Height / 6);
					return;
				}
				WindowState = FormWindowState.Normal;
				Location = (Point)new Size(Screen.PrimaryScreen.Bounds.Width / 2 - Screen.PrimaryScreen.Bounds.Width / 10, Screen.PrimaryScreen.Bounds.Height / 2 - Screen.PrimaryScreen.Bounds.Height / 6);
				Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
				return;
			}
		}

		private void Form1_FormClosing(object sender, FormClosedEventArgs e)
		{
			WindowState = FormWindowState.Minimized;
			Visible = false;
		}

		public void InitMinimize()
		{
			var menuItem = new MenuItem();
			var menuItem2 = new MenuItem();
			new MenuItem();
			var menuItem3 = new MenuItem();
			var menuItem4 = new MenuItem();
			new MenuItem();
			var menuItem5 = new MenuItem();
			try
			{
				var menuItems = new[]
				{
					menuItem,
					menuItem2,
					menuItem3,
					menuItem5,
					menuItem4
				};
				menuItem.Text = "显示";
				menuItem.Click += tray_show_Click;
				menuItem2.Text = "设置";
				menuItem2.Click += tray_Set_Click;
				menuItem3.Text = "更新";
				menuItem3.Click += tray_update_Click;
				menuItem5.Text = "帮助";
				menuItem5.Click += tray_help_Click;
				menuItem4.Text = "退出";
				menuItem4.Click += tray_exit_Click;
				minico.ContextMenu = new ContextMenu(menuItems);
			}
			catch (Exception ex)
			{
				MessageBox.Show("InitMinimize()" + ex.Message);
			}
		}

		private void tray_show_Click(object sender, EventArgs e)
		{
			base.Show();
			Activate();
			Visible = true;
			WindowState = FormWindowState.Normal;
			if (IniHelp.GetValue("工具栏", "顶置") == "True")
			{
				TopMost = true;
				return;
			}
			TopMost = false;
		}

		private void tray_exit_Click(object sender, EventArgs e)
		{
			minico.Dispose();
			saveIniFile();
			Process.GetCurrentProcess().Kill();
		}

		private void setting_Click(object sender, EventArgs e)
		{
		}

		private void Form1_LostFocus(object sender, EventArgs e)
		{
		}

		private void Main_copy_Click(object sender, EventArgs e)
		{
			RichBoxBody.Focus();
			RichBoxBody.richTextBox1.Copy();
		}

		public static string punctuation_en_ch(string text)
		{
			var array = text.ToCharArray();
			for (var i = 0; i < array.Length; i++)
			{
				var num = ":;,?!()".IndexOf(array[i]);
				if (num != -1)
				{
					array[i] = "：；，？！（）"[num];
				}
			}
			return new string(array);
		}

		private void Main_SelectAll_Click(object sender, EventArgs e)
		{
			RichBoxBody.Focus();
			RichBoxBody.richTextBox1.SelectAll();
		}

		private void Main_paste_Click(object sender, EventArgs e)
		{
			RichBoxBody.Focus();
			RichBoxBody.richTextBox1.Paste();
		}

		public void Split_Click(object sender, EventArgs e)
		{
			RichBoxBody.Text = split_txt;
		}

		public static byte[] copybyte(byte[] a, byte[] b)
		{
			var array = new byte[a.Length + b.Length];
			a.CopyTo(array, 0);
			b.CopyTo(array, a.Length);
			return array;
		}

		public void OCR_sougou()
		{
			try
			{
				split_txt = "";
				var image = image_screen;
				var i = image.Width;
				var j = image.Height;
				if (i < 300)
				{
					while (i < 300)
					{
						j *= 2;
						i *= 2;
					}
				}
				if (j < 120)
				{
					while (j < 120)
					{
						j *= 2;
						i *= 2;
					}
				}
				var bitmap = new Bitmap(i, j);
				var graphics = Graphics.FromImage(bitmap);
				graphics.DrawImage(image, 0, 0, i, j);
				graphics.Save();
				graphics.Dispose();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(OCR_sougou_SogouOCR(bitmap)))["result"].ToString());
				bitmap.Dispose();
				checked_txt(jarray, 2, "content");
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public byte[] ImageTobyte(Image imgPhoto)
		{
			var memoryStream = new MemoryStream();
			imgPhoto.Save(memoryStream, ImageFormat.Jpeg);
			var array = new byte[memoryStream.Length];
			memoryStream.Position = 0L;
			memoryStream.Read(array, 0, array.Length);
			memoryStream.Close();
			return array;
		}

		private Bitmap GetPlus(Bitmap bm, double times)
		{
			var width = (int)(bm.Width / times);
			var height = (int)(bm.Height / times);
			var bitmap = new Bitmap(width, height);
			if (times >= 1.0 && times <= 1.1)
			{
				bitmap = bm;
			}
			else
			{
				var graphics = Graphics.FromImage(bitmap);
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.DrawImage(bm, new Rectangle(0, 0, width, height), new Rectangle(0, 0, bm.Width, bm.Height), GraphicsUnit.Pixel);
				graphics.Dispose();
			}
			return bitmap;
		}

		public void OCR_Tencent()
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundaryRDEqU0w702X9cWPJ";
				var image = image_screen;
				if (image.Width > 90 && image.Height < 90)
				{
					var bitmap = new Bitmap(image.Width, 300);
					var graphics = Graphics.FromImage(bitmap);
					graphics.DrawImage(image, 5, 0, image.Width, image.Height);
					graphics.Save();
					graphics.Dispose();
					image = new Bitmap(bitmap);
				}
				else if (image.Width <= 90 && image.Height >= 90)
				{
					var bitmap2 = new Bitmap(300, image.Height);
					var graphics2 = Graphics.FromImage(bitmap2);
					graphics2.DrawImage(image, 0, 5, image.Width, image.Height);
					graphics2.Save();
					graphics2.Dispose();
					image = new Bitmap(bitmap2);
				}
				else if (image.Width < 90 && image.Height < 90)
				{
					var bitmap3 = new Bitmap(300, 300);
					var graphics3 = Graphics.FromImage(bitmap3);
					graphics3.DrawImage(image, 5, 5, image.Width, image.Height);
					graphics3.Save();
					graphics3.Dispose();
					image = new Bitmap(bitmap3);
				}
				else
				{
					image = image_screen;
				}
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"image_file\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://ai.qq.com/cgi-bin/appdemo_generalocr");
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "http://ai.qq.com/product/ocr.shtml";
				httpWebRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				httpWebRequest.Timeout = 8000;
				httpWebRequest.ReadWriteTimeout = 2000;
				var buffer = array;
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(buffer, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["item_list"].ToString());
				checked_txt(jarray, 1, "itemstring");
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public void OCR_baidu_bak()
		{
			split_txt = "";
			try
			{
				var str = "CHN_ENG";
				split_txt = "";
				var image = image_screen;
				var memoryStream = new MemoryStream();
				image.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				if (interface_flag == "中英")
				{
					str = "CHN_ENG";
				}
				if (interface_flag == "日语")
				{
					str = "JAP";
				}
				if (interface_flag == "韩语")
				{
					str = "KOR";
				}
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var str2 = "";
				var str3 = "";
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					var array2 = jobject["words"].ToString().ToCharArray();
					if (!char.IsPunctuation(array2[array2.Length - 1]))
					{
						if (!contain_ch(jobject["words"].ToString()))
						{
							str3 = str3 + jobject["words"].ToString().Trim() + " ";
						}
						else
						{
							str3 += jobject["words"].ToString();
						}
					}
					else if (own_punctuation(array2[array2.Length - 1].ToString()))
					{
						if (!contain_ch(jobject["words"].ToString()))
						{
							str3 = str3 + jobject["words"].ToString().Trim() + " ";
						}
						else
						{
							str3 += jobject["words"].ToString();
						}
					}
					else
					{
						str3 = str3 + jobject["words"] + "\r\n";
					}
					str2 = str2 + jobject["words"] + "\r\n";
				}
				split_txt = str2;
				typeset_txt = str3;
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		private void OCR_sougou_Click(object sender, EventArgs e)
		{
			OCR_foreach("搜狗");
		}

		private void OCR_tencent_Click(object sender, EventArgs e)
		{
			OCR_foreach("腾讯");
		}

		private void OCR_baidu_Click(object sender, EventArgs e)
		{
		}

		public void OCR_youdao()
		{
			try
			{
				split_txt = "";
				var image = image_screen;
				if (image.Width > 90 && image.Height < 90)
				{
					var bitmap = new Bitmap(image.Width, 200);
					var graphics = Graphics.FromImage(bitmap);
					graphics.DrawImage(image, 5, 0, image.Width, image.Height);
					graphics.Save();
					graphics.Dispose();
					image = new Bitmap(bitmap);
				}
				else if (image.Width <= 90 && image.Height >= 90)
				{
					var bitmap2 = new Bitmap(200, image.Height);
					var graphics2 = Graphics.FromImage(bitmap2);
					graphics2.DrawImage(image, 0, 5, image.Width, image.Height);
					graphics2.Save();
					graphics2.Dispose();
					image = new Bitmap(bitmap2);
				}
				else if (image.Width < 90 && image.Height < 90)
				{
					var bitmap3 = new Bitmap(200, 200);
					var graphics3 = Graphics.FromImage(bitmap3);
					graphics3.DrawImage(image, 5, 5, image.Width, image.Height);
					graphics3.Save();
					graphics3.Dispose();
					image = new Bitmap(bitmap3);
				}
				else
				{
					image = image_screen;
				}
				var i = image.Width;
				var j = image.Height;
				if (i < 600)
				{
					while (i < 600)
					{
						j *= 2;
						i *= 2;
					}
				}
				if (j < 120)
				{
					while (j < 120)
					{
						j *= 2;
						i *= 2;
					}
				}
				var bitmap4 = new Bitmap(i, j);
				var graphics4 = Graphics.FromImage(bitmap4);
				graphics4.DrawImage(image, 0, 0, i, j);
				graphics4.Save();
				graphics4.Dispose();
				image = new Bitmap(bitmap4);
				var inArray = OCR_ImgToByte(image);
				var s = "imgBase=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(inArray)) + "&lang=auto&company=";
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://aidemo.youdao.com/ocrapi1");
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "http://aidemo.youdao.com/ocrdemo";
				httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest.Timeout = 8000;
				httpWebRequest.ReadWriteTimeout = 2000;
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["lines"].ToString());
				checked_txt(jarray, 1, "words");
				image.Dispose();
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public void OCR_youdao_Click(object sender, EventArgs e)
		{
			OCR_foreach("有道");
		}

		public void change_Chinese_Click(object sender, EventArgs e)
		{
			language = "中文标点";
			if (typeset_txt != "")
			{
				RichBoxBody.Text = punctuation_en_ch_x(RichBoxBody.Text);
				RichBoxBody.Text = punctuation_quotation(RichBoxBody.Text);
			}
		}

		public void change_English_Click(object sender, EventArgs e)
		{
			language = "英文标点";
			if (typeset_txt != "")
			{
				RichBoxBody.Text = punctuation_ch_en(RichBoxBody.Text);
			}
		}

		public static string punctuation_ch_en(string text)
		{
			var array = text.ToCharArray();
			for (var i = 0; i < array.Length; i++)
			{
				var num = "：。；，？！“”‘’【】（）".IndexOf(array[i]);
				if (num != -1)
				{
					array[i] = ":.;,?!\"\"''[]()"[num];
				}
			}
			return new string(array);
		}

		public void saveIniFile()
		{
			IniHelp.SetValue("配置", "接口", interface_flag);
		}

		public void readIniFile()
		{
			Proxy_flag = IniHelp.GetValue("代理", "代理类型");
			Proxy_url = IniHelp.GetValue("代理", "服务器");
			Proxy_port = IniHelp.GetValue("代理", "端口");
			Proxy_name = IniHelp.GetValue("代理", "服务器账号");
			Proxy_password = IniHelp.GetValue("代理", "服务器密码");
			if (Proxy_flag == "不使用代理")
			{
				WebRequest.DefaultWebProxy = null;
			}
			if (Proxy_flag == "系统代理")
			{
				WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
			}
			if (Proxy_flag == "自定义代理")
			{
				try
				{
					var webProxy = new WebProxy(Proxy_url, Convert.ToInt32(Proxy_port));
					if (Proxy_name != "" && Proxy_password != "")
					{
						ICredentials credentials = new NetworkCredential(Proxy_name, Proxy_password);
						webProxy.Credentials = credentials;
					}
					WebRequest.DefaultWebProxy = webProxy;
				}
				catch
				{
					MessageBox.Show("请检查代理设置！");
				}
			}
			interface_flag = IniHelp.GetValue("配置", "接口");
			if (interface_flag == "发生错误")
			{
				IniHelp.SetValue("配置", "接口", "搜狗");
				OCR_foreach("搜狗");
			}
			else
			{
				OCR_foreach(interface_flag);
			}
			var filePath = AppDomain.CurrentDomain.BaseDirectory + "Data\\config.ini";
			if (IniHelp.GetValue("快捷键", "文字识别") != "请按下快捷键")
			{
				var value = IniHelp.GetValue("快捷键", "文字识别");
				var text = "None";
				var text2 = "F4";
				SetHotkey(text, text2, value, 200);
			}
			if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
			{
				var value2 = IniHelp.GetValue("快捷键", "翻译文本");
				var text3 = "None";
				var text4 = "F7";
				SetHotkey(text3, text4, value2, 205);
			}
			if (IniHelp.GetValue("快捷键", "记录界面") != "请按下快捷键")
			{
				var value3 = IniHelp.GetValue("快捷键", "记录界面");
				var text5 = "None";
				var text6 = "F8";
				SetHotkey(text5, text6, value3, 206);
			}
			if (IniHelp.GetValue("快捷键", "识别界面") != "请按下快捷键")
			{
				var value4 = IniHelp.GetValue("快捷键", "识别界面");
				var text7 = "None";
				var text8 = "F11";
				SetHotkey(text7, text8, value4, 235);
			}
			StaticValue.baiduAPI_ID = HelpWin32.IniFileHelper.GetValue("密钥_百度", "secret_id", filePath);
			if (HelpWin32.IniFileHelper.GetValue("密钥_百度", "secret_id", filePath) == "发生错误")
			{
				StaticValue.baiduAPI_ID = "请输入secret_id";
			}
			StaticValue.baiduAPI_key = HelpWin32.IniFileHelper.GetValue("密钥_百度", "secret_key", filePath);
			if (HelpWin32.IniFileHelper.GetValue("密钥_百度", "secret_key", filePath) == "发生错误")
			{
				StaticValue.baiduAPI_key = "请输入secret_key";
			}
		}

		public static string check_ch_en(string text)
		{
			var array = text.ToCharArray();
			for (var i = 0; i < array.Length; i++)
			{
				var num = "：".IndexOf(array[i]);
				if (num != -1 && i - 1 >= 0 && i + 1 < array.Length && contain_en(array[i - 1].ToString()) && contain_en(array[i + 1].ToString()))
				{
					array[i] = ":"[num];
				}
				if (num != -1 && i - 1 >= 0 && i + 1 < array.Length && contain_en(array[i - 1].ToString()) && contain_punctuation(array[i + 1].ToString()))
				{
					array[i] = ":"[num];
				}
			}
			return new string(array);
		}

		public void tray_Set_Click(object sender, EventArgs e)
		{
			var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			HelpWin32.UnregisterHotKey(Handle, 200);
			HelpWin32.UnregisterHotKey(Handle, 205);
			HelpWin32.UnregisterHotKey(Handle, 206);
			HelpWin32.UnregisterHotKey(Handle, 235);
			WindowState = FormWindowState.Minimized;
			var fmSetting = new FmSetting();
			fmSetting.TopMost = true;
			fmSetting.ShowDialog();
			if (fmSetting.DialogResult == DialogResult.OK)
			{
				var filePath = AppDomain.CurrentDomain.BaseDirectory + "Data\\config.ini";
				StaticValue.v_notecount = Convert.ToInt32(HelpWin32.IniFileHelper.GetValue("配置", "记录数目", filePath));
				pubnote = new string[StaticValue.v_notecount];
				for (var i = 0; i < StaticValue.v_notecount; i++)
				{
					pubnote[i] = "";
				}
				StaticValue.v_note = pubnote;
				fmnote.Text_note_change = "";
				fmnote.Location = new Point(Screen.AllScreens[0].WorkingArea.Width - fmnote.Width, Screen.AllScreens[0].WorkingArea.Height - fmnote.Height);
				if (IniHelp.GetValue("快捷键", "文字识别") != "请按下快捷键")
				{
					var value = IniHelp.GetValue("快捷键", "文字识别");
					var text = "None";
					var text2 = "F4";
					SetHotkey(text, text2, value, 200);
				}
				if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
				{
					var value2 = IniHelp.GetValue("快捷键", "翻译文本");
					var text3 = "None";
					var text4 = "F9";
					SetHotkey(text3, text4, value2, 205);
				}
				if (IniHelp.GetValue("快捷键", "记录界面") != "请按下快捷键")
				{
					var value3 = IniHelp.GetValue("快捷键", "记录界面");
					var text5 = "None";
					var text6 = "F8";
					SetHotkey(text5, text6, value3, 206);
				}
				if (IniHelp.GetValue("快捷键", "识别界面") != "请按下快捷键")
				{
					var value4 = IniHelp.GetValue("快捷键", "识别界面");
					var text7 = "None";
					var text8 = "F11";
					SetHotkey(text7, text8, value4, 235);
				}
				Proxy_flag = IniHelp.GetValue("代理", "代理类型");
				Proxy_url = IniHelp.GetValue("代理", "服务器");
				Proxy_port = IniHelp.GetValue("代理", "端口");
				Proxy_name = IniHelp.GetValue("代理", "服务器账号");
				Proxy_password = IniHelp.GetValue("代理", "服务器密码");
				StaticValue.baiduAPI_ID = IniHelp.GetValue("密钥_百度", "secret_id");
				StaticValue.baiduAPI_key = IniHelp.GetValue("密钥_百度", "secret_key");
				if (Proxy_flag == "不使用代理")
				{
					WebRequest.DefaultWebProxy = null;
				}
				if (Proxy_flag == "系统代理")
				{
					WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
				}
				if (Proxy_flag == "自定义代理")
				{
					try
					{
						var webProxy = new WebProxy(Proxy_url, Convert.ToInt32(Proxy_port));
						if (Proxy_name != "" && Proxy_password != "")
						{
							ICredentials credentials = new NetworkCredential(Proxy_name, Proxy_password);
							webProxy.Credentials = credentials;
						}
						WebRequest.DefaultWebProxy = webProxy;
					}
					catch
					{
						MessageBox.Show("请检查代理设置！");
					}
				}
				if (IniHelp.GetValue("更新", "更新间隔") == "True")
				{
					Program.checkTimer.Enabled = true;
					Program.checkTimer.Interval = 3600000.0 * Convert.ToInt32(IniHelp.GetValue("更新", "间隔时间"));
					Program.checkTimer.Elapsed += Program.CheckTimer_Elapsed;
					Program.checkTimer.Start();
				}
			}
		}

		public void tray_limit_Click(object sender, EventArgs e)
		{
			new Thread(about).Start();
		}

		public static bool IsNum(string str)
		{
			for (var i = 0; i < str.Length; i++)
			{
				if (str[i] < '0' || str[i] > '9')
				{
					return false;
				}
			}
			return true;
		}

		public bool own_punctuation(string text)
		{
			return ",;，、<>《》()-（）.。".IndexOf(text) != -1;
		}

		public static string punctuation_Del_space(string text)
		{
			var pattern = "(?<=.)([^\\*]+)(?=.)";
			string result;
			if (Regex.Match(text, pattern).ToString().IndexOf(" ") >= 0)
			{
				text = Regex.Replace(text, "(?<=[\\p{P}*])([a-zA-Z])(?=[a-zA-Z])", " $1");
				char[] trimChars = null;
				text = text.TrimEnd(trimChars).Replace("- ", "-").Replace("_ ", "_").Replace("( ", "(").Replace("/ ", "/").Replace("\" ", "\"");
				result = text;
			}
			else
			{
				result = text;
			}
			return result;
		}

		public static bool contain_ch(string str)
		{
			return Regex.IsMatch(str, "[\\u4e00-\\u9fa5]");
		}

		public void transtalate_Click()
		{
			typeset_txt = RichBoxBody.Text;
			RichBoxBody_T.Visible = true;
			WindowState = FormWindowState.Normal;
			transtalate_fla = "开启";
			RichBoxBody.Dock = DockStyle.None;
			RichBoxBody_T.Dock = DockStyle.None;
			RichBoxBody_T.BorderStyle = BorderStyle.Fixed3D;
			RichBoxBody_T.Text = "";
			RichBoxBody.Focus();
			if (num_ok == 0)
			{
				RichBoxBody.Size = new Size(ClientRectangle.Width, ClientRectangle.Height);
				Size = new Size(RichBoxBody.Width * 2, RichBoxBody.Height);
				RichBoxBody_T.Size = new Size(RichBoxBody.Width, RichBoxBody.Height);
				RichBoxBody_T.Location = (Point)new Size(RichBoxBody.Width, 0);
				RichBoxBody_T.Name = "rich_trans";
				RichBoxBody_T.TabIndex = 1;
				RichBoxBody_T.Text_flag = "我是翻译文本框";
				RichBoxBody_T.ImeMode = ImeMode.On;
			}
			num_ok++;
			PictureBox1.Visible = true;
			PictureBox1.BringToFront();
			MinimumSize = new Size((int)font_base.Width * 23 * 2, (int)font_base.Height * 24);
			Size = new Size((int)font_base.Width * 23 * 2, (int)font_base.Height * 24);
			CheckForIllegalCrossThreadCalls = false;
			new Thread(trans_Calculate).Start();
		}

		private void Form_Resize(object sender, EventArgs e)
		{
			if (RichBoxBody.Dock != DockStyle.Fill)
			{
				RichBoxBody.Size = new Size(ClientRectangle.Width / 2, ClientRectangle.Height);
				RichBoxBody_T.Size = new Size(RichBoxBody.Width, ClientRectangle.Height);
				RichBoxBody_T.Location = (Point)new Size(RichBoxBody.Width, 0);
			}
		}

		public void Trans_copy_Click(object sender, EventArgs e)
		{
			RichBoxBody_T.Focus();
			RichBoxBody_T.richTextBox1.Copy();
		}

		public void Trans_paste_Click(object sender, EventArgs e)
		{
			RichBoxBody_T.Focus();
			RichBoxBody_T.richTextBox1.Paste();
		}

		public void Trans_SelectAll_Click(object sender, EventArgs e)
		{
			RichBoxBody_T.Focus();
			RichBoxBody_T.richTextBox1.SelectAll();
		}

		public void trans_Calculate()
		{
			if (pinyin_flag)
			{
				googleTranslate_txt = PinYin.ToPinYin(typeset_txt);
			}
			else if (typeset_txt == "")
			{
				googleTranslate_txt = "";
			}
			else
			{
				if (interface_flag == "韩语")
				{
					StaticValue.zh_en = false;
					StaticValue.zh_jp = false;
					StaticValue.zh_ko = true;
					RichBoxBody_T.set_language = "韩语";
				}
				if (interface_flag == "日语")
				{
					StaticValue.zh_en = false;
					StaticValue.zh_jp = true;
					StaticValue.zh_ko = false;
					RichBoxBody_T.set_language = "日语";
				}
				if (interface_flag == "中英")
				{
					StaticValue.zh_en = true;
					StaticValue.zh_jp = false;
					StaticValue.zh_ko = false;
					RichBoxBody_T.set_language = "中英";
				}
				if (IniHelp.GetValue("配置", "翻译接口") == "谷歌")
				{
					googleTranslate_txt = Translate_Google(typeset_txt);
				}
				if (IniHelp.GetValue("配置", "翻译接口") == "百度")
				{
					googleTranslate_txt = Translate_baidu(typeset_txt);
				}
				if (IniHelp.GetValue("配置", "翻译接口") == "腾讯")
				{
					googleTranslate_txt = Translate_Tencent(typeset_txt);
				}
			}
			PictureBox1.Visible = false;
			PictureBox1.SendToBack();
			Invoke(new translate(translate_child));
			pinyin_flag = false;
		}

		public void Trans_close_Click(object sender, EventArgs e)
		{
			base.MinimumSize = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
			transtalate_fla = "关闭";
			RichBoxBody.Dock = DockStyle.Fill;
			RichBoxBody_T.Visible = false;
			PictureBox1.Visible = false;
			RichBoxBody_T.Text = "";
			if (WindowState == FormWindowState.Maximized)
			{
				WindowState = FormWindowState.Normal;
			}
			Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
		}

		private void ShowLoading()
		{
			try
			{
				fmloading = new Fmloading();
				Application.Run(fmloading);
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
			finally
			{
				thread.Abort();
			}
		}

		public bool contain(string text, string subStr)
		{
			return text.Contains(subStr);
		}

		public static bool contain_en(string str)
		{
			return Regex.IsMatch(str, "[a-zA-Z]");
		}

		public static bool punctuation_has_punctuation(string str)
		{
			string pattern;
			if (contain_ch(str))
			{
				pattern = "[\\；\\，\\。\\！\\？]";
			}
			else
			{
				pattern = "[\\;\\,\\.\\!\\?]";
			}
			return Regex.IsMatch(str, pattern);
		}

		public string quotation(string str)
		{
			return Regex.Replace(str.Replace("“", "\"").Replace("”", "\""), "(?<=\")([^\\\"\\“\\”]+)(?=\")", "$1_测_$2");
		}

		private string punctuation_quotation(string pStr)
		{
			pStr = pStr.Replace("“", "\"").Replace("”", "\"");
			var array = pStr.Split('"');
			var text = "";
			for (var i = 1; i <= array.Length; i++)
			{
				if (i % 2 == 0)
				{
					text = text + array[i - 1] + "”";
				}
				else
				{
					text = text + array[i - 1] + "“";
				}
			}
			return text.Substring(0, text.Length - 1);
		}

		public static bool HasenPunctuation(string str)
		{
			var pattern = "[\\;\\,\\.\\!\\?]";
			return Regex.IsMatch(str, pattern);
		}

		public static string Del_Space(string text)
		{
			text = Regex.Replace(text, "([\\p{P}]+)", "**&&**$1**&&**");
			char[] trimChars = null;
			text = text.TrimEnd(trimChars).Replace(" **&&**", "").Replace("**&&** ", "").Replace("**&&**", "");
			return text;
		}

		public void TTS()
		{
			new Thread(TTS_thread).Start();
		}

		public void about()
		{
			WindowState = FormWindowState.Minimized;
			CheckForIllegalCrossThreadCalls = false;
			new Thread(ThreadFun).Start();
		}

		private void ThreadFun()
		{
		}

		private void translate_child()
		{
			RichBoxBody_T.Text = googleTranslate_txt;
			googleTranslate_txt = "";
		}

		public void TTS_thread()
		{
			try
			{
				var responseStream = ((HttpWebResponse)((HttpWebRequest)WebRequest.Create(string.Format("{0}?{1}", "http://aip.baidubce.com/oauth/2.0/token", "grant_type=client_credentials&client_id=iQekhH39WqHoxur5ss59GpU4&client_secret=8bcee1cee76ed60cdfaed1f2c038584d"))).GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				string str;
				if (!contain_ch(htmltxt))
				{
					str = "zh";
				}
				else
				{
					str = "zh";
				}
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Concat("http://tsn.baidu.com/text2audio?lan=" + str + "&ctp=1&cuid=abcdxxx&tok=", ((JObject)JsonConvert.DeserializeObject(value))["access_token"].ToString(), "&tex=", HttpUtility.UrlEncode(htmltxt.Replace("***", "")), "&vol=9&per=0&spd=5&pit=5"));
				httpWebRequest.Method = "POST";
				var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				var array = new byte[16384];
				byte[] array2;
				using (var memoryStream = new MemoryStream())
				{
					int count;
					while ((count = httpWebResponse.GetResponseStream().Read(array, 0, array.Length)) > 0)
					{
						memoryStream.Write(array, 0, count);
					}
					array2 = memoryStream.ToArray();
				}
				ttsData = array2;
				if (speak_copyb == "朗读" || voice_count == 0)
				{
					Invoke(new translate(Speak_child));
					speak_copyb = "";
				}
				else
				{
					Invoke(new translate(TTS_child));
				}
				voice_count++;
			}
			catch (Exception ex)
			{
				if (ex.ToString().IndexOf("Null") <= -1)
				{
					MessageBox.Show("文本过长，请使用右键菜单中的选中朗读！", "提醒");
				}
			}
		}

		public void TTS_child()
		{
			if (RichBoxBody.Text != null || RichBoxBody_T.Text != "")
			{
				if (speaking)
				{
					HelpWin32.mciSendString("close media", null, 0, IntPtr.Zero);
					speaking = false;
					return;
				}
				var tempPath = Path.GetTempPath();
				var text = tempPath + "\\声音.mp3";
				try
				{
					File.WriteAllBytes(text, ttsData);
				}
				catch
				{
					text = tempPath + "\\声音1.mp3";
					File.WriteAllBytes(text, ttsData);
				}
				PlaySong(text);
				speaking = true;
			}
		}

		// (get) Token: 0x0600009A RID: 154 RVA: 0x000025D2 File Offset: 0x000007D2
		protected override CreateParams CreateParams
		{
			get
			{
				var createParams = base.CreateParams;
				createParams.ExStyle |= 134217728;
				return createParams;
			}
		}

		public string GetGoogletHtml(string url, CookieContainer cookie, string refer)
		{
			var text = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "GET";
			httpWebRequest.CookieContainer = cookie;
			httpWebRequest.Referer = refer;
			httpWebRequest.Timeout = 5000;
			httpWebRequest.Headers.Add("X-Requested-With:XMLHttpRequest");
			httpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
			httpWebRequest.UserAgent = "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 65.0.3325.146 Safari / 537.36";
			string result;
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public string Translate_Google(string text)
		{
			var text2 = "";
			try
			{
				var text3 = "zh-CN";
				var text4 = "en";
				if (StaticValue.zh_en)
				{
					if (ch_count(text.Trim()) > en_count(text.Trim()) || (en_count(text.Trim()) == 1 && ch_count(text.Trim()) == 1))
					{
						text3 = "zh-CN";
						text4 = "en";
					}
					else
					{
						text3 = "en";
						text4 = "zh-CN";
					}
				}
				if (StaticValue.zh_jp)
				{
					if (contain_jap(repalceStr(Del_ch(text.Trim()))))
					{
						text3 = "ja";
						text4 = "zh-CN";
					}
					else
					{
						text3 = "zh-CN";
						text4 = "ja";
					}
				}
				if (StaticValue.zh_ko)
				{
					if (contain_kor(text.Trim()))
					{
						text3 = "ko";
						text4 = "zh-CN";
					}
					else
					{
						text3 = "zh-CN";
						text4 = "ko";
					}
				}
				var httpHelper = new HttpHelper();
				var item = new HttpItem
				{
					URL = "https://translate.google.cn/translate_a/single",
					Method = "POST",
					ContentType = "application/x-www-form-urlencoded; charset=UTF-8",
					Postdata = string.Concat("client=gtx&sl=", text3, "&tl=", text4, "&dt=t&q=", HttpUtility.UrlEncode(text).Replace("+", "%20")),
					UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)",
					Accept = "*/*"
				};
				var jarray = (JArray)JsonConvert.DeserializeObject(httpHelper.GetHtml(item).Html);
				var count = ((JArray)jarray[0]).Count;
				for (var i = 0; i < count; i++)
				{
					text2 += jarray[0][i][0].ToString();
				}
			}
			catch (Exception)
			{
				text2 = "[谷歌接口报错]：\r\n1.网络错误或者文本过长。\r\n2.谷歌接口可能对于某些网络不能用，具体不清楚。可以尝试挂VPN试试。\r\n3.这个问题我没办法修复，请右键菜单更换百度、腾讯翻译接口。";
			}
			return text2;
		}

		public static string CookieCollectionToStrCookie(CookieCollection cookie)
		{
			string result;
			if (cookie == null)
			{
				result = string.Empty;
			}
			else
			{
				var text = string.Empty;
				foreach (var obj in cookie)
				{
					var cookie2 = (Cookie)obj;
					text += string.Format("{0}={1};", cookie2.Name, cookie2.Value);
				}
				result = text;
			}
			return result;
		}

		public string ScanQRCode()
		{
			var result = "";
			try
			{
				var image = new BinaryBitmap(new HybridBinarizer(new BitmapLuminanceSource((Bitmap)image_screen)));
				var result2 = new QRCodeReader().decode(image);
				if (result2 != null)
				{
					result = result2.Text;
				}
			}
			catch
			{
			}
			return result;
		}

		public void Main_baidu_search(object sender, EventArgs e)
		{
			if (RichBoxBody.SelectText == "")
			{
				Process.Start("https://www.baidu.com/");
				return;
			}
			Process.Start("https://www.baidu.com/s?wd=" + RichBoxBody.SelectText);
		}

		public void tray_update_Click(object sender, EventArgs e)
		{
			Program.CheckUpdate();
		}

		public static bool contain_jap(string str)
		{
			return Regex.IsMatch(str, "[\\u3040-\\u309F]") || Regex.IsMatch(str, "[\\u30A0-\\u30FF]");
		}

		public static bool contain_kor(string str)
		{
			return Regex.IsMatch(str, "[\\uac00-\\ud7ff]");
		}

		public static string Del_ch(string str)
		{
			var text = str;
			if (Regex.IsMatch(str, "[\\u4e00-\\u9fa5]"))
			{
				text = string.Empty;
				var array = str.ToCharArray();
				for (var i = 0; i < array.Length; i++)
				{
					if (array[i] < '一' || array[i] > '龥')
					{
						text += array[i].ToString();
					}
				}
			}
			return text;
		}

		public static string repalceStr(string hexData)
		{
			return Regex.Replace(hexData, "[\\p{P}+~$`^=|<>～｀＄＾＋＝｜＜＞￥×┊ ]", "").ToUpper();
		}

		public static string RemovePunctuation(string str)
		{
			str = str.Replace(",", "").Replace("，", "").Replace(".", "").Replace("。", "").Replace("!", "").Replace("！", "").Replace("?", "").Replace("？", "").Replace(":", "").Replace("：", "").Replace(";", "").Replace("；", "").Replace("～", "").Replace("-", "").Replace("_", "").Replace("——", "").Replace("—", "").Replace("--", "").Replace("【", "").Replace("】", "").Replace("\\", "").Replace("(", "").Replace(")", "").Replace("（", "").Replace("）", "").Replace("#", "").Replace("$", "").Replace("、", "").Replace("‘", "").Replace("’", "").Replace("“", "").Replace("”", "");
			return str;
		}

		public static string GetUniqueFileName(string fullName)
		{
			string result;
			if (!File.Exists(fullName))
			{
				result = fullName;
			}
			else
			{
				var directoryName = Path.GetDirectoryName(fullName);
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);
				var extension = Path.GetExtension(fullName);
				var num = 1;
				string text;
				do
				{
					text = Path.Combine(directoryName, string.Format("{0}[{1}].{2}", fileNameWithoutExtension, num++, extension));
				}
				while (File.Exists(text));
				result = text;
			}
			return result;
		}

		public static string ReFileName(string strFolderPath, string strFileName)
		{
			var text = strFolderPath + "\\" + strFileName;
			var startIndex = text.LastIndexOf('.');
			text = text.Insert(startIndex, "_{0}");
			var num = 1;
			var path = string.Format(text, num);
			while (File.Exists(path))
			{
				path = string.Format(text, num);
				num++;
			}
			return Path.GetFileName(path);
		}

		public void PlaySong(string file)
		{
			HelpWin32.mciSendString("close media", null, 0, IntPtr.Zero);
			HelpWin32.mciSendString("open \"" + file + "\" type mpegvideo alias media", null, 0, IntPtr.Zero);
			HelpWin32.mciSendString("play media notify", null, 0, Handle);
		}

		public void Main_Voice_Click(object sender, EventArgs e)
		{
			RichBoxBody.Focus();
			speak_copyb = "朗读";
			htmltxt = RichBoxBody.SelectText;
			HelpWin32.SendMessage(Handle, 786, 590);
		}

		public void Trans_Voice_Click(object sender, EventArgs e)
		{
			RichBoxBody_T.Focus();
			speak_copyb = "朗读";
			htmltxt = RichBoxBody_T.SelectText;
			HelpWin32.SendMessage(Handle, 786, 590);
		}

		public void Speak_child()
		{
			if (RichBoxBody.Text != null || RichBoxBody_T.Text != "")
			{
				var tempPath = Path.GetTempPath();
				var text = tempPath + "\\声音.mp3";
				try
				{
					File.WriteAllBytes(text, ttsData);
				}
				catch
				{
					text = tempPath + "\\声音1.mp3";
					File.WriteAllBytes(text, ttsData);
				}
				PlaySong(text);
				speaking = true;
			}
		}

		public static string ToSimplified(string source)
		{
			var text = new string(' ', source.Length);
			HelpWin32.LCMapString(2048, 33554432, source, source.Length, text, source.Length);
			return text;
		}

		public static string ToTraditional(string source)
		{
			var text = new string(' ', source.Length);
			HelpWin32.LCMapString(2048, 67108864, source, source.Length, text, source.Length);
			return text;
		}

		public void change_zh_tra_Click(object sender, EventArgs e)
		{
			if (RichBoxBody.Text != null)
			{
				RichBoxBody.Text = ToTraditional(RichBoxBody.Text);
			}
		}

		public void change_tra_zh_Click(object sender, EventArgs e)
		{
			if (RichBoxBody.Text != null)
			{
				RichBoxBody.Text = ToSimplified(RichBoxBody.Text);
			}
		}

		public void change_str_Upper_Click(object sender, EventArgs e)
		{
			if (RichBoxBody.Text != null)
			{
				RichBoxBody.Text = RichBoxBody.Text.ToUpper();
			}
		}

		public void change_Upper_str_Click(object sender, EventArgs e)
		{
			if (RichBoxBody.Text != null)
			{
				RichBoxBody.Text = RichBoxBody.Text.ToLower();
			}
		}

		public string[] hotkey(string text, string text2, string value)
		{
			var array = (value + "+").Split('+');
			if (array.Length == 3)
			{
				text = array[0];
				text2 = array[1];
			}
			if (array.Length == 2)
			{
				text = "None";
				text2 = value;
			}
			return new[]
			{
				text,
				text2
			};
		}

		public void SetHotkey(string text, string text2, string value, int flag)
		{
			var array = (value + "+").Split('+');
			if (array.Length == 3)
			{
				text = array[0];
				text2 = array[1];
			}
			if (array.Length == 2)
			{
				text = "None";
				text2 = value;
			}
			var array2 = new[]
			{
				text,
				text2
			};
			if (!HelpWin32.RegisterHotKey(Handle, flag, (HelpWin32.KeyModifiers)Enum.Parse(typeof(HelpWin32.KeyModifiers), array2[0].Trim()), (Keys)Enum.Parse(typeof(Keys), array2[1].Trim())))
			{
				fmflags.Show();
				fmflags.DrawStr("快捷键冲突，请更换！");
			}
			HelpWin32.RegisterHotKey(Handle, flag, (HelpWin32.KeyModifiers)Enum.Parse(typeof(HelpWin32.KeyModifiers), array2[0].Trim()), (Keys)Enum.Parse(typeof(Keys), array2[1].Trim()));
		}

		public void bool_error()
		{
		}

		public void p_note(string a)
		{
			for (var i = 0; i < StaticValue.v_notecount; i++)
			{
				if (i == StaticValue.v_notecount - 1)
				{
					pubnote[StaticValue.v_notecount - 1] = a;
				}
				else
				{
					pubnote[i] = pubnote[i + 1];
				}
			}
		}

		public void tray_note_Click(object sender, EventArgs e)
		{
			fmnote.Show();
			fmnote.WindowState = FormWindowState.Normal;
			fmnote.Visible = true;
		}

		public string Google_Hotkey(string text)
		{
			var text2 = "";
			try
			{
				string text3;
				string text4;
				if (contain_ch(trans_hotkey.Trim()))
				{
					text3 = "zh-CN";
					text4 = "en";
				}
				else
				{
					text3 = "en";
					text4 = "zh-CN";
				}
				var url = string.Concat("https://translate.google.cn/translate_a/single?client=gtx&sl=", text3, "&tl=", text4, "&dt=t&q=", HttpUtility.UrlEncode(text).Replace("+", "%20"));
				var jarray = (JArray)JsonConvert.DeserializeObject(Get_GoogletHtml(url));
				var count = ((JArray)jarray[0]).Count;
				for (var i = 0; i < count; i++)
				{
					text2 += jarray[0][i][0].ToString();
				}
			}
			catch (Exception ex)
			{
				text2 = "[Error]:" + ex.Message;
			}
			return text2;
		}

		private string GetTextFromClipboard()
		{
			if (Thread.CurrentThread.GetApartmentState() > ApartmentState.STA)
			{
				var thread = new Thread(delegate()
				{
					SendKeys.SendWait("^c");
					SendKeys.Flush();
				});
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
				thread.Join();
			}
			else
			{
				SendKeys.SendWait("^c");
				SendKeys.Flush();
			}
			var text = Clipboard.GetText();
			text = (string.IsNullOrWhiteSpace(text) ? null : text);
			if (text != null)
			{
				Clipboard.Clear();
			}
			return text;
		}

		public static string Get_html(string url)
		{
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						result = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				httpWebRequest.Abort();
			}
			catch
			{
				result = "";
			}
			return result;
		}

		public CookieContainer Post_Html_Getcookie(string url, string post_str)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 5000;
			httpWebRequest.Headers.Add("Accept-Language:zh-CN,zh;q=0.8");
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			httpWebRequest.CookieContainer = new CookieContainer();
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return httpWebRequest.CookieContainer;
		}

		public string Post_Html_Reccookie(string url, string post_str, CookieContainer CookieContainer)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 6000;
			httpWebRequest.Headers.Add("Accept-Language:zh-CN,zh;q=0.8");
			httpWebRequest.Headers.Add("Accept-Encoding: gzip, deflate");
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			httpWebRequest.CookieContainer = CookieContainer;
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		public string Post_Html(string url, string post_str)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 6000;
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			httpWebRequest.Headers.Add("Accept-Language: zh-CN,en,*");
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		public void Main_OCR_Quickscreenshots()
		{
			if (!StaticValue.截图排斥)
			{
				try
				{
					change_QQ_screenshot = false;
					FormBorderStyle = FormBorderStyle.None;
					Visible = false;
					Thread.Sleep(100);
					if (transtalate_fla == "开启")
					{
						form_width = Width / 2;
					}
					else
					{
						form_width = Width;
					}
					shupai_Right_txt = "";
					shupai_Left_txt = "";
					form_height = Height;
					minico.Visible = false;
					minico.Visible = true;
					menu.Close();
					menu_copy.Close();
					auto_fla = "开启";
					split_txt = "";
					RichBoxBody.Text = "***该区域未发现文本***";
					RichBoxBody_T.Text = "";
					typeset_txt = "";
					transtalate_fla = "关闭";
					if (IniHelp.GetValue("工具栏", "翻译") == "False")
					{
						Trans_close.PerformClick();
					}
					Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
					FormBorderStyle = FormBorderStyle.Sizable;
					StaticValue.截图排斥 = true;
					string mode_flag;
					Point point;
					Rectangle[] buildRects;
					image_screen = RegionCaptureTasks.GetRegionImage_Mo(new RegionCaptureOptions
					{
						ShowMagnifier = false,
						UseSquareMagnifier = false,
						MagnifierPixelCount = 15,
						MagnifierPixelSize = 10
					}, out mode_flag, out point, out buildRects);
					if (mode_flag == "高级截图")
					{
						var mode = RegionCaptureMode.Annotation;
						var options = new RegionCaptureOptions();
						using (var regionCaptureForm = new RegionCaptureForm(mode, options))
						{
							regionCaptureForm.Image_get = false;
							regionCaptureForm.Prepare(image_screen);
							regionCaptureForm.ShowDialog();
							image_screen = null;
							image_screen = regionCaptureForm.GetResultImage();
							mode_flag = regionCaptureForm.Mode_flag;
						}
					}
					HelpWin32.RegisterHotKey(Handle, 222, HelpWin32.KeyModifiers.None, Keys.Escape);
					if (mode_flag == "贴图")
					{
						var locationPoint = new Point(point.X, point.Y);
						new FmScreenPaste(image_screen, locationPoint).Show();
						if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
						{
							var value = IniHelp.GetValue("快捷键", "翻译文本");
							var text = "None";
							var text2 = "F9";
							SetHotkey(text, text2, value, 205);
						}
						HelpWin32.UnregisterHotKey(Handle, 222);
						StaticValue.截图排斥 = false;
					}
					else if (mode_flag == "区域多选")
					{
						if (image_screen == null)
						{
							if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
							{
								var value2 = IniHelp.GetValue("快捷键", "翻译文本");
								var text3 = "None";
								var text4 = "F9";
								SetHotkey(text3, text4, value2, 205);
							}
							HelpWin32.UnregisterHotKey(Handle, 222);
							StaticValue.截图排斥 = false;
						}
						else
						{
							minico.Visible = true;
							thread = new Thread(ShowLoading);
							thread.Start();
							ts = new TimeSpan(DateTime.Now.Ticks);
							getSubPics_ocr(image_screen, buildRects);
						}
					}
					else if (mode_flag == "取色")
					{
						if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
						{
							var value3 = IniHelp.GetValue("快捷键", "翻译文本");
							var text5 = "None";
							var text6 = "F9";
							SetHotkey(text5, text6, value3, 205);
						}
						HelpWin32.UnregisterHotKey(Handle, 222);
						StaticValue.截图排斥 = false;
						fmflags.Show();
						fmflags.DrawStr("已复制颜色");
					}
					else if (image_screen == null)
					{
						if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
						{
							var value4 = IniHelp.GetValue("快捷键", "翻译文本");
							var text7 = "None";
							var text8 = "F9";
							SetHotkey(text7, text8, value4, 205);
						}
						HelpWin32.UnregisterHotKey(Handle, 222);
						StaticValue.截图排斥 = false;
					}
					else
					{
						if (mode_flag == "百度")
						{
							baidu_flags = "百度";
						}
						if (mode_flag == "拆分")
						{
							set_merge = false;
							set_split = true;
						}
						if (mode_flag == "合并")
						{
							set_merge = true;
							set_split = false;
						}
						if (mode_flag == "截图")
						{
							Clipboard.SetImage(image_screen);
							if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
							{
								var value5 = IniHelp.GetValue("快捷键", "翻译文本");
								var text9 = "None";
								var text10 = "F9";
								SetHotkey(text9, text10, value5, 205);
							}
							HelpWin32.UnregisterHotKey(Handle, 222);
							StaticValue.截图排斥 = false;
							if (IniHelp.GetValue("截图音效", "粘贴板") == "True")
							{
								PlaySong(IniHelp.GetValue("截图音效", "音效路径"));
							}
							fmflags.Show();
							fmflags.DrawStr("已复制截图");
						}
						else if (mode_flag == "自动保存" && IniHelp.GetValue("配置", "自动保存") == "True")
						{
							var filename = IniHelp.GetValue("配置", "截图位置") + "\\" + ReFileName(IniHelp.GetValue("配置", "截图位置"), "图片.Png");
							image_screen.Save(filename, ImageFormat.Png);
							StaticValue.截图排斥 = false;
							if (IniHelp.GetValue("截图音效", "自动保存") == "True")
							{
								PlaySong(IniHelp.GetValue("截图音效", "音效路径"));
							}
							fmflags.Show();
							fmflags.DrawStr("已保存图片");
						}
						else if (mode_flag == "多区域自动保存" && IniHelp.GetValue("配置", "自动保存") == "True")
						{
							getSubPics(image_screen, buildRects);
							StaticValue.截图排斥 = false;
							if (IniHelp.GetValue("截图音效", "自动保存") == "True")
							{
								PlaySong(IniHelp.GetValue("截图音效", "音效路径"));
							}
							fmflags.Show();
							fmflags.DrawStr("已保存图片");
						}
						else if (mode_flag == "保存")
						{
							var saveFileDialog = new SaveFileDialog();
							saveFileDialog.Filter = "png图片(*.png)|*.png|jpg图片(*.jpg)|*.jpg|bmp图片(*.bmp)|*.bmp";
							saveFileDialog.AddExtension = false;
							saveFileDialog.FileName = string.Concat("tianruo_", DateTime.Now.Year.ToString(), "-", DateTime.Now.Month.ToString(), "-", DateTime.Now.Day.ToString(), "-", DateTime.Now.Ticks.ToString());
							saveFileDialog.Title = "保存图片";
							saveFileDialog.FilterIndex = 1;
							saveFileDialog.RestoreDirectory = true;
							if (saveFileDialog.ShowDialog() == DialogResult.OK)
							{
								var extension = Path.GetExtension(saveFileDialog.FileName);
								if (extension.Equals(".jpg"))
								{
									image_screen.Save(saveFileDialog.FileName, ImageFormat.Jpeg);
								}
								if (extension.Equals(".png"))
								{
									image_screen.Save(saveFileDialog.FileName, ImageFormat.Png);
								}
								if (extension.Equals(".bmp"))
								{
									image_screen.Save(saveFileDialog.FileName, ImageFormat.Bmp);
								}
							}
							if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
							{
								var value6 = IniHelp.GetValue("快捷键", "翻译文本");
								var text11 = "None";
								var text12 = "F9";
								SetHotkey(text11, text12, value6, 205);
							}
							HelpWin32.UnregisterHotKey(Handle, 222);
							StaticValue.截图排斥 = false;
						}
						else if (image_screen != null)
						{
							if (IniHelp.GetValue("工具栏", "分栏") == "True")
							{
								minico.Visible = true;
								thread = new Thread(ShowLoading);
								thread.Start();
								ts = new TimeSpan(DateTime.Now.Ticks);
								var image = image_screen;
								var graphics = Graphics.FromImage(new Bitmap(image.Width, image.Height));
								graphics.DrawImage(image, 0, 0, image.Width, image.Height);
								graphics.Save();
								graphics.Dispose();
								((Bitmap)FindBundingBox_fences((Bitmap)image)).Save("Data\\分栏预览图.jpg");
								image.Dispose();
								image_screen.Dispose();
							}
							else
							{
								minico.Visible = true;
								thread = new Thread(ShowLoading);
								thread.Start();
								ts = new TimeSpan(DateTime.Now.Ticks);
								var messageload = new Messageload();
								messageload.ShowDialog();
								if (messageload.DialogResult == DialogResult.OK)
								{
									esc_thread = new Thread(Main_OCR_Thread);
									esc_thread.Start();
								}
							}
						}
					}
				}
				catch
				{
					StaticValue.截图排斥 = false;
				}
			}
		}

		public void Main_OCR_Thread()
		{
			if (ScanQRCode() != "")
			{
				typeset_txt = ScanQRCode();
				RichBoxBody.Text = typeset_txt;
				fmloading.fml_close = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "搜狗")
			{
				OCR_sougou2();
				fmloading.fml_close = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "腾讯")
			{
				OCR_Tencent();
				fmloading.fml_close = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "有道")
			{
				OCR_youdao();
				fmloading.fml_close = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "公式")
			{
				OCR_Math();
				fmloading.fml_close = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "百度表格")
			{
				OCR_baidu_table();
				fmloading.fml_close = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_table));
				return;
			}
			if (interface_flag == "阿里表格")
			{
				OCR_ali_table();
				fmloading.fml_close = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_table));
				return;
			}
			if (interface_flag == "日语" || interface_flag == "中英" || interface_flag == "韩语")
			{
				OCR_baidu();
				fmloading.fml_close = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
			}
			if (interface_flag == "从左向右" || interface_flag == "从右向左")
			{
				shupai_Right_txt = "";
				var image = image_screen;
				var bitmap = new Bitmap(image.Width, image.Height);
				var graphics = Graphics.FromImage(bitmap);
				graphics.DrawImage(image, 0, 0, image.Width, image.Height);
				graphics.Save();
				graphics.Dispose();
				image_ori = bitmap;
				var image2 = new Image<Gray, byte>(bitmap);
				var image3 = new Image<Gray, byte>((Bitmap)FindBundingBox(image2.ToBitmap()));
				var draw = image3.Convert<Bgr, byte>();
				var image4 = image3.Clone();
				CvInvoke.Canny(image3, image4, 0.0, 0.0, 5, true);
				select_image(image4, draw);
				bitmap.Dispose();
				image2.Dispose();
				image3.Dispose();
			}
			image_screen.Dispose();
			GC.Collect();
		}

		public void Main_OCR_Thread_last()
		{
			image_screen.Dispose();
			GC.Collect();
			StaticValue.截图排斥 = false;
			var text = typeset_txt;
			text = check_str(text);
			split_txt = check_str(split_txt);
			if (!punctuation_has_punctuation(text))
			{
				text = split_txt;
			}
			if (contain_ch(text.Trim()))
			{
				text = Del_Space(text);
			}
			if (text != "")
			{
				RichBoxBody.Text = text;
			}
			StaticValue.v_Split = split_txt;
			if (bool.Parse(IniHelp.GetValue("工具栏", "拆分")) || set_split)
			{
				set_split = false;
				RichBoxBody.Text = split_txt;
			}
			if (bool.Parse(IniHelp.GetValue("工具栏", "合并")) || set_merge)
			{
				set_merge = false;
				RichBoxBody.Text = text.Replace("\n", "").Replace("\r", "");
			}
			var timeSpan = new TimeSpan(DateTime.Now.Ticks);
			var timeSpan2 = timeSpan.Subtract(ts).Duration();
			var str = string.Concat(new[]
			{
				timeSpan2.Seconds.ToString(),
				".",
				Convert.ToInt32(timeSpan2.TotalMilliseconds).ToString(),
				"秒"
			});
			if (RichBoxBody.Text != null)
			{
				p_note(RichBoxBody.Text);
				StaticValue.v_note = pubnote;
				if (fmnote.Created)
				{
					fmnote.Text_note = "";
				}
			}
			if (StaticValue.v_topmost)
			{
				TopMost = true;
			}
			else
			{
				TopMost = false;
			}
			Text = "耗时：" + str;
			minico.Visible = true;
			if (interface_flag == "从右向左")
			{
				RichBoxBody.Text = shupai_Right_txt;
			}
			if (interface_flag == "从左向右")
			{
				RichBoxBody.Text = shupai_Left_txt;
			}
			Clipboard.SetDataObject(RichBoxBody.Text);
			if (baidu_flags == "百度")
			{
				FormBorderStyle = FormBorderStyle.Sizable;
				Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
				Visible = false;
				WindowState = FormWindowState.Minimized;
				base.Show();
				Process.Start("https://www.baidu.com/s?wd=" + RichBoxBody.Text);
				baidu_flags = "";
				if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
				{
					var value = IniHelp.GetValue("快捷键", "翻译文本");
					var text2 = "None";
					var text3 = "F9";
					SetHotkey(text2, text3, value, 205);
				}
				HelpWin32.UnregisterHotKey(Handle, 222);
				return;
			}
			if (IniHelp.GetValue("配置", "识别弹窗") == "False")
			{
				FormBorderStyle = FormBorderStyle.Sizable;
				Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
				Visible = false;
				fmflags.Show();
				if (RichBoxBody.Text == "***该区域未发现文本***")
				{
					fmflags.DrawStr("无文本");
				}
				else
				{
					fmflags.DrawStr("已识别");
				}
				if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
				{
					var value2 = IniHelp.GetValue("快捷键", "翻译文本");
					var text4 = "None";
					var text5 = "F9";
					SetHotkey(text4, text5, value2, 205);
				}
				HelpWin32.UnregisterHotKey(Handle, 222);
				return;
			}
			FormBorderStyle = FormBorderStyle.Sizable;
			Visible = true;
			base.Show();
			WindowState = FormWindowState.Normal;
			Size = new Size(form_width, form_height);
			HelpWin32.SetForegroundWindow(Handle);
			StaticValue.v_googleTranslate_txt = RichBoxBody.Text;
			if (bool.Parse(IniHelp.GetValue("工具栏", "翻译")))
			{
				try
				{
					auto_fla = "";
					Invoke(new translate(transtalate_Click));
				}
				catch
				{
				}
			}
			if (bool.Parse(IniHelp.GetValue("工具栏", "检查")))
			{
				try
				{
					RichBoxBody.Find = "";
				}
				catch
				{
				}
			}
			if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
			{
				var value3 = IniHelp.GetValue("快捷键", "翻译文本");
				var text6 = "None";
				var text7 = "F9";
				SetHotkey(text6, text7, value3, 205);
			}
			HelpWin32.UnregisterHotKey(Handle, 222);
			RichBoxBody.Refresh();
		}

		private void OCR_baidu_Ch_and_En_Click(object sender, EventArgs e)
		{
			OCR_foreach("中英");
		}

		private void OCR_baidu_Jap_Click(object sender, EventArgs e)
		{
			OCR_foreach("日语");
		}

		private void OCR_baidu_Kor_Click(object sender, EventArgs e)
		{
			OCR_foreach("韩语");
		}

		public string Get_GoogletHtml(string url)
		{
			var text = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "GET";
			httpWebRequest.Timeout = 5000;
			httpWebRequest.Headers.Add("Accept-Language: zh-CN;q=0.8,en-US;q=0.6,en;q=0.4");
			httpWebRequest.Headers.Add("Accept-Encoding: gzip,deflate");
			httpWebRequest.Headers.Add("Accept-Charset: utf-8");
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
			httpWebRequest.Host = "translate.google.cn";
			httpWebRequest.Accept = "*/*";
			httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
			string result;
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public void OCR_baidu()
		{
			split_txt = "";
			try
			{
				baidu_vip = Get_html(string.Format("{0}?{1}", "https://aip.baidubce.com/oauth/2.0/token", "grant_type=client_credentials&client_id=" + StaticValue.baiduAPI_ID + "&client_secret=" + StaticValue.baiduAPI_key));
				if (baidu_vip == "")
				{
					MessageBox.Show("请检查密钥输入是否正确！", "提醒");
				}
				else
				{
					var str = "CHN_ENG";
					split_txt = "";
					var img = image_screen;
					var inArray = OCR_ImgToByte(img);
					if (interface_flag == "中英")
					{
						str = "CHN_ENG";
					}
					if (interface_flag == "日语")
					{
						str = "JAP";
					}
					if (interface_flag == "韩语")
					{
						str = "KOR";
					}
					var s = "image=" + HttpUtility.UrlEncode(Convert.ToBase64String(inArray)) + "&language_type=" + str;
					var bytes = Encoding.UTF8.GetBytes(s);
					var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic?access_token=" + ((JObject)JsonConvert.DeserializeObject(baidu_vip))["access_token"]);
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
					httpWebRequest.Timeout = 8000;
					httpWebRequest.ReadWriteTimeout = 5000;
					using (var requestStream = httpWebRequest.GetRequestStream())
					{
						requestStream.Write(bytes, 0, bytes.Length);
					}
					var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
					var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
					responseStream.Close();
					var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["words_result"].ToString());
					checked_txt(jarray, 1, "words");
				}
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本或者密钥次数用尽***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public string check_str(string text)
		{
			if (contain_ch(text.Trim()))
			{
				text = punctuation_en_ch(text.Trim());
				text = check_ch_en(text.Trim());
			}
			else
			{
				text = punctuation_ch_en(text.Trim());
				if (contain(text, ".") && (contain(text, ",") || contain(text, "!") || contain(text, "(") || contain(text, ")") || contain(text, "'")))
				{
					text = punctuation_Del_space(text);
				}
			}
			return text;
		}

		public static string punctuation_en_ch_x(string text)
		{
			var array = text.ToCharArray();
			for (var i = 0; i < array.Length; i++)
			{
				var num = ".:;,?![]()".IndexOf(array[i]);
				if (num != -1)
				{
					array[i] = "。：；，？！【】（）"[num];
				}
			}
			return new string(array);
		}

		public string OCR_sougou_SogouPost(string url, CookieContainer cookie, byte[] content)
		{
			var text = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.CookieContainer = cookie;
			httpWebRequest.Timeout = 10000;
			httpWebRequest.Referer = "http://pic.sogou.com/resource/pic/shitu_intro/index.html";
			httpWebRequest.ContentType = "multipart/form-data; boundary=----WebKitFormBoundary1ZZDB9E4sro7pf0g";
			httpWebRequest.Accept = "*/*";
			httpWebRequest.Headers.Add("Origin: http://pic.sogou.com");
			httpWebRequest.Headers.Add("Accept-Encoding: gzip,deflate");
			httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
			httpWebRequest.ServicePoint.Expect100Continue = false;
			httpWebRequest.ProtocolVersion = new Version(1, 1);
			httpWebRequest.ContentLength = content.Length;
			var requestStream = httpWebRequest.GetRequestStream();
			requestStream.Write(content, 0, content.Length);
			requestStream.Close();
			string result;
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					var stream = httpWebResponse.GetResponseStream();
					if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
					{
						stream = new GZipStream(stream, CompressionMode.Decompress);
					}
					using (var streamReader = new StreamReader(stream, Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public string OCR_sougou_SogouGet(string url, CookieContainer cookie, string refer)
		{
			var text = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "GET";
			httpWebRequest.CookieContainer = cookie;
			httpWebRequest.Referer = refer;
			httpWebRequest.Timeout = 10000;
			httpWebRequest.Accept = "application/json";
			httpWebRequest.Headers.Add("X-Requested-With: XMLHttpRequest");
			httpWebRequest.Headers.Add("Accept-Encoding: gzip,deflate");
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
			httpWebRequest.ServicePoint.Expect100Continue = false;
			httpWebRequest.ProtocolVersion = new Version(1, 1);
			string result;
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					var stream = httpWebResponse.GetResponseStream();
					if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
					{
						stream = new GZipStream(stream, CompressionMode.Decompress);
					}
					using (var streamReader = new StreamReader(stream, Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public string OCR_sougou_SogouOCR(Image img)
		{
			var cookie = new CookieContainer();
			var url = "http://pic.sogou.com/pic/upload_pic.jsp";
			var str = OCR_sougou_SogouPost(url, cookie, OCR_sougou_Content_Length(img));
			var url2 = "http://pic.sogou.com/pic/ocr/ocrOnline.jsp?query=" + str;
			var refer = "http://pic.sogou.com/resource/pic/shitu_intro/word_1.html?keyword=" + str;
			return OCR_sougou_SogouGet(url2, cookie, refer);
		}

		private byte[] OCR_ImgToByte(Image img)
		{
			byte[] result;
			try
			{
				var memoryStream = new MemoryStream();
				img.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				result = array;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public byte[] OCR_sougou_Content_Length(Image img)
		{
			var bytes = Encoding.UTF8.GetBytes("------WebKitFormBoundary1ZZDB9E4sro7pf0g\r\nContent-Disposition: form-data; name=\"pic_path\"; filename=\"test2018.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n");
			var array = OCR_ImgToByte(img);
			var bytes2 = Encoding.UTF8.GetBytes("\r\n------WebKitFormBoundary1ZZDB9E4sro7pf0g--\r\n");
			var array2 = new byte[bytes.Length + array.Length + bytes2.Length];
			bytes.CopyTo(array2, 0);
			array.CopyTo(array2, bytes.Length);
			bytes2.CopyTo(array2, bytes.Length + array.Length);
			return array2;
		}

		public void OCR_sougou2()
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundary8orYTmcj8BHvQpVU";
				Image image = ZoomImage((Bitmap)image_screen, 120, 120);
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"pic\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ocr.shouji.sogou.com/v2/ocr/json");
				httpWebRequest.Timeout = 8000;
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(array, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["result"].ToString());
				if (IniHelp.GetValue("工具栏", "分段") == "True")
				{
					checked_location_sougou(jarray, 2, "content", "frame");
				}
				else
				{
					checked_txt(jarray, 2, "content");
				}
				image.Dispose();
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public static byte[] Mergebyte(byte[] a, byte[] b, byte[] c)
		{
			var array = new byte[a.Length + b.Length + c.Length];
			a.CopyTo(array, 0);
			b.CopyTo(array, a.Length);
			c.CopyTo(array, a.Length + b.Length);
			return array;
		}

		public static bool contain_punctuation(string str)
		{
			return Regex.IsMatch(str, "\\p{P}");
		}

		private void tray_help_Click(object sender, EventArgs e)
		{
			WindowState = FormWindowState.Minimized;
			new FmHelp().Show();
		}

		public bool Is_punctuation(string text)
		{
			return ",;:，（）、；".IndexOf(text) != -1;
		}

		public bool has_punctuation(string text)
		{
			return ",;，；、<>《》()-（）".IndexOf(text) != -1;
		}

		public void checked_txt(JArray jarray, int lastlength, string words)
		{
			var num = 0;
			for (var i = 0; i < jarray.Count; i++)
			{
				var length = JObject.Parse(jarray[i].ToString())[words].ToString().Length;
				if (length > num)
				{
					num = length;
				}
			}
			var str = "";
			var text = "";
			for (var j = 0; j < jarray.Count - 1; j++)
			{
				var jobject = JObject.Parse(jarray[j].ToString());
				var array = jobject[words].ToString().ToCharArray();
				var jobject2 = JObject.Parse(jarray[j + 1].ToString());
				var array2 = jobject2[words].ToString().ToCharArray();
				var length2 = jobject[words].ToString().Length;
				var length3 = jobject2[words].ToString().Length;
				if (Math.Abs(length2 - length3) <= 0)
				{
					if (split_paragraph(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else
					{
						text += jobject[words].ToString().Trim();
					}
				}
				else if (split_paragraph(array[array.Length - lastlength].ToString()) && Math.Abs(length2 - length3) <= 1)
				{
					if (split_paragraph(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else
					{
						text += jobject[words].ToString().Trim();
					}
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && length2 <= num / 2)
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()) && length3 - length2 < 4 && array2[1].ToString() == ".")
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_en(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text = text + jobject[words].ToString().Trim() + " ";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_en(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (Is_punctuation(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (Is_punctuation(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text = text + jobject[words].ToString().Trim() + " ";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (IsNum(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (IsNum(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				if (has_punctuation(jobject[words].ToString()))
				{
					text += "\r\n";
				}
				str = str + jobject[words].ToString().Trim() + "\r\n";
			}
			split_txt = str + JObject.Parse(jarray[jarray.Count - 1].ToString())[words];
			typeset_txt = text.Replace("\r\n\r\n", "\r\n") + JObject.Parse(jarray[jarray.Count - 1].ToString())[words];
		}

		private void OCR_foreach(string name)
		{
			var filePath = AppDomain.CurrentDomain.BaseDirectory + "Data\\config.ini";
			if (name == "韩语")
			{
				interface_flag = "韩语";
				Refresh();
				baidu.Text = "百度√";
				kor.Text = "韩语√";
			}
			if (name == "日语")
			{
				interface_flag = "日语";
				Refresh();
				baidu.Text = "百度√";
				jap.Text = "日语√";
			}
			if (name == "中英")
			{
				interface_flag = "中英";
				Refresh();
				baidu.Text = "百度√";
				ch_en.Text = "中英√";
			}
			if (name == "搜狗")
			{
				interface_flag = "搜狗";
				Refresh();
				sougou.Text = "搜狗√";
			}
			if (name == "腾讯")
			{
				interface_flag = "腾讯";
				Refresh();
				tencent.Text = "腾讯√";
			}
			if (name == "有道")
			{
				interface_flag = "有道";
				Refresh();
				youdao.Text = "有道√";
			}
			if (name == "公式")
			{
				interface_flag = "公式";
				Refresh();
				Mathfuntion.Text = "公式√";
			}
			if (name == "百度表格")
			{
				interface_flag = "百度表格";
				Refresh();
				ocr_table.Text = "表格√";
				baidu_table.Text = "百度√";
			}
			if (name == "阿里表格")
			{
				interface_flag = "阿里表格";
				Refresh();
				ocr_table.Text = "表格√";
				ali_table.Text = "阿里√";
			}
			if (name == "从左向右")
			{
				if (!File.Exists("cvextern.dll"))
				{
					MessageBox.Show("请从蓝奏网盘中下载cvextern.dll大小约25m，点击确定自动弹出网页。\r\n将下载后的文件与 天若OCR文字识别.exe 这个文件放在一起。");
					Process.Start("https://www.lanzous.com/i1ab3vg");
				}
				else
				{
					interface_flag = "从左向右";
					Refresh();
					shupai.Text = "竖排√";
					left_right.Text = "从左向右√";
				}
			}
			if (name == "从右向左")
			{
				if (!File.Exists("cvextern.dll"))
				{
					MessageBox.Show("请从蓝奏网盘中下载cvextern.dll大小约25m，点击确定自动弹出网页。\r\n将下载后的文件与 天若OCR文字识别.exe 这个文件放在一起。");
					Process.Start("https://www.lanzous.com/i1ab3vg");
					return;
				}
				interface_flag = "从右向左";
				Refresh();
				shupai.Text = "竖排√";
				righ_left.Text = "从右向左√";
			}
			HelpWin32.IniFileHelper.SetValue("配置", "接口", interface_flag, filePath);
		}

		private void OCR_shupai_Click(object sender, EventArgs e)
		{
		}

		private void OCR_write_Click(object sender, EventArgs e)
		{
			OCR_foreach("手写");
		}

		private void OCR_lefttoright_Click(object sender, EventArgs e)
		{
			OCR_foreach("从左向右");
		}

		private void OCR_righttoleft_Click(object sender, EventArgs e)
		{
			OCR_foreach("从右向左");
		}

		public void OCR_baidu_acc()
		{
			split_txt = "";
			var text = "";
			try
			{
				baidu_vip = Get_html(string.Format("{0}?{1}", "https://aip.baidubce.com/oauth/2.0/token", "grant_type=client_credentials&client_id=" + StaticValue.baiduAPI_ID + "&client_secret=" + StaticValue.baiduAPI_key));
				if (baidu_vip == "")
				{
					MessageBox.Show("请检查密钥输入是否正确！", "提醒");
				}
				else
				{
					split_txt = "";
					var img = image_screen;
					var inArray = OCR_ImgToByte(img);
					var s = "image=" + HttpUtility.UrlEncode(Convert.ToBase64String(inArray));
					var bytes = Encoding.UTF8.GetBytes(s);
					var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic?access_token=" + ((JObject)JsonConvert.DeserializeObject(baidu_vip))["access_token"]);
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
					httpWebRequest.Timeout = 8000;
					httpWebRequest.ReadWriteTimeout = 5000;
					ServicePointManager.DefaultConnectionLimit = 512;
					using (var requestStream = httpWebRequest.GetRequestStream())
					{
						requestStream.Write(bytes, 0, bytes.Length);
					}
					var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
					var value = text = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
					responseStream.Close();
					var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["words_result"].ToString());
					var text2 = "";
					for (var i = 0; i < jarray.Count; i++)
					{
						var jobject = JObject.Parse(jarray[i].ToString());
						text2 += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					}
					shupai_Right_txt = shupai_Right_txt + text2 + "\r\n";
					Thread.Sleep(600);
				}
			}
			catch
			{
				MessageBox.Show(text, "提醒");
				StaticValue.截图排斥 = false;
				esc = "退出";
				fmloading.fml_close = "窗体已关闭";
				esc_thread.Abort();
			}
		}

		public void OCR_Tencent_handwriting()
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundaryRDEqU0w702X9cWPJ";
				var image = image_screen;
				if (image.Width > 90 && image.Height < 90)
				{
					var bitmap = new Bitmap(image.Width, 300);
					var graphics = Graphics.FromImage(bitmap);
					graphics.DrawImage(image, 5, 0, image.Width, image.Height);
					graphics.Save();
					graphics.Dispose();
					image = new Bitmap(bitmap);
				}
				else if (image.Width <= 90 && image.Height >= 90)
				{
					var bitmap2 = new Bitmap(300, image.Height);
					var graphics2 = Graphics.FromImage(bitmap2);
					graphics2.DrawImage(image, 0, 5, image.Width, image.Height);
					graphics2.Save();
					graphics2.Dispose();
					image = new Bitmap(bitmap2);
				}
				else if (image.Width < 90 && image.Height < 90)
				{
					var bitmap3 = new Bitmap(300, 300);
					var graphics3 = Graphics.FromImage(bitmap3);
					graphics3.DrawImage(image, 5, 5, image.Width, image.Height);
					graphics3.Save();
					graphics3.Dispose();
					image = new Bitmap(bitmap3);
				}
				else
				{
					image = image_screen;
				}
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"image_file\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://ai.qq.com/cgi-bin/appdemo_handwritingocr");
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "http://ai.qq.com/product/ocr.shtml";
				httpWebRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				httpWebRequest.Timeout = 8000;
				httpWebRequest.ReadWriteTimeout = 2000;
				var buffer = array;
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(buffer, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["item_list"].ToString());
				checked_txt(jarray, 1, "itemstring");
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public Image BoundingBox(Image<Gray, byte> src, Image<Bgr, byte> draw)
		{
			Image result;
			using (var vectorOfVectorOfPoint = new VectorOfVectorOfPoint())
			{
				CvInvoke.FindContours(src, vectorOfVectorOfPoint, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, default(Point));
				Image image = draw.ToBitmap();
				var graphics = Graphics.FromImage(image);
				var size = vectorOfVectorOfPoint.Size;
				for (var i = 0; i < size; i++)
				{
					using (var vectorOfPoint = vectorOfVectorOfPoint[i])
					{
						var rectangle = CvInvoke.BoundingRectangle(vectorOfPoint);
						var x = rectangle.Location.X;
						var y = rectangle.Location.Y;
						var width = rectangle.Size.Width;
						var height = rectangle.Size.Height;
						if (width > 5 || height > 5)
						{
							graphics.FillRectangle(Brushes.White, x, 0, width, image.Size.Height);
						}
					}
				}
				graphics.Dispose();
				var bitmap = new Bitmap(image.Width + 2, image.Height + 2);
				var graphics2 = Graphics.FromImage(bitmap);
				graphics2.DrawImage(image, 1, 1, image.Width, image.Height);
				graphics2.Save();
				graphics2.Dispose();
				result = bitmap;
			}
			return result;
		}

		public void select_image(Image<Gray, byte> src, Image<Bgr, byte> draw)
		{
			try
			{
				using (var vectorOfVectorOfPoint = new VectorOfVectorOfPoint())
				{
					CvInvoke.FindContours(src, vectorOfVectorOfPoint, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, default(Point));
					var num = vectorOfVectorOfPoint.Size / 2;
					imagelist_lenght = num;
					bool_image_count(num);
					if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Data\\image_temp"))
					{
						Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Data\\image_temp");
					}
					OCR_baidu_a = "";
					OCR_baidu_b = "";
					OCR_baidu_c = "";
					OCR_baidu_d = "";
					OCR_baidu_e = "";
					for (var i = 0; i < num; i++)
					{
						using (var vectorOfPoint = vectorOfVectorOfPoint[i])
						{
							var rectangle = CvInvoke.BoundingRectangle(vectorOfPoint);
							if (rectangle.Size.Width > 1 && rectangle.Size.Height > 1)
							{
								var x = rectangle.Location.X;
								var y = rectangle.Location.Y;
								var width = rectangle.Size.Width;
								var height = rectangle.Size.Height;
								new Point(x, 0);
								new Point(x, image_ori.Size.Height);
								var srcRect = new Rectangle(x, 0, width, image_ori.Size.Height);
								var bitmap = new Bitmap(width + 70, srcRect.Size.Height);
								var graphics = Graphics.FromImage(bitmap);
								graphics.FillRectangle(Brushes.White, 0, 0, bitmap.Size.Width, bitmap.Size.Height);
								graphics.DrawImage(image_ori, 30, 0, srcRect, GraphicsUnit.Pixel);
								var bitmap2 = Image.FromHbitmap(bitmap.GetHbitmap());
								bitmap2.Save("Data\\image_temp\\" + i + ".jpg", ImageFormat.Jpeg);
								bitmap2.Dispose();
								bitmap.Dispose();
								graphics.Dispose();
							}
						}
					}
					var messageload = new Messageload();
					messageload.ShowDialog();
					if (messageload.DialogResult == DialogResult.OK)
					{
						var array = new[]
						{
							new ManualResetEvent(false)
						};
						ThreadPool.QueueUserWorkItem(DoWork, array[0]);
					}
				}
			}
			catch
			{
				exit_thread();
			}
		}

		public Image FindBundingBox(Bitmap bitmap)
		{
			var image = new Image<Bgr, byte>(bitmap);
			var image2 = new Image<Gray, byte>(image.Width, image.Height);
			CvInvoke.CvtColor(image, image2, ColorConversion.Bgra2Gray, 0);
			var structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(4, 4), new Point(1, 1));
			CvInvoke.Erode(image2, image2, structuringElement, new Point(0, 2), 1, BorderType.Reflect101, default(MCvScalar));
			CvInvoke.Threshold(image2, image2, 100.0, 255.0, (ThresholdType)9);
			var image3 = new Image<Gray, byte>(image2.ToBitmap());
			var draw = image3.Convert<Bgr, byte>();
			var image4 = image3.Clone();
			CvInvoke.Canny(image3, image4, 255.0, 255.0, 5, true);
			return BoundingBox(image4, draw);
		}

		public void Captureimage(int width, Image g_image, string saveFilePath, Rectangle rect)
		{
			var bitmap = new Bitmap(width + 70, g_image.Size.Height);
			var graphics = Graphics.FromImage(bitmap);
			graphics.FillRectangle(Brushes.White, 0, 0, bitmap.Size.Width, bitmap.Size.Height);
			graphics.DrawImage(g_image, 30, 0, rect, GraphicsUnit.Pixel);
			var bitmap2 = Image.FromHbitmap(bitmap.GetHbitmap());
			bitmap2.Save(saveFilePath, ImageFormat.Jpeg);
			image_screen = bitmap2;
			OCR_baidu_use();
			bitmap2.Dispose();
			bitmap.Dispose();
			graphics.Dispose();
		}

		public void OCR_baidu_use()
		{
			split_txt = "";
			try
			{
				var str = "CHN_ENG";
				split_txt = "";
				var image = image_screen;
				var memoryStream = new MemoryStream();
				image.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var text2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					text2 += array2[j];
				}
				shupai_Right_txt = (shupai_Right_txt + text + "\r\n").Replace("\r\n\r\n", "");
				shupai_Left_txt = text2.Replace("\r\n\r\n", "");
				MessageBox.Show(shupai_Left_txt);
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_sougou_use()
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundary8orYTmcj8BHvQpVU";
				var image = image_screen;
				var i = image.Width;
				var j = image.Height;
				if (i < 300)
				{
					while (i < 300)
					{
						j *= 2;
						i *= 2;
					}
				}
				if (j < 120)
				{
					while (j < 120)
					{
						j *= 2;
						i *= 2;
					}
				}
				var bitmap = new Bitmap(i, j);
				var graphics = Graphics.FromImage(bitmap);
				graphics.DrawImage(image, 0, 0, i, j);
				graphics.Save();
				graphics.Dispose();
				image = new Bitmap(bitmap);
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"pic\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ocr.shouji.sogou.com/v2/ocr/json");
				httpWebRequest.Timeout = 8000;
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(array, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["result"].ToString());
				var text2 = "";
				for (var k = 0; k < jarray.Count; k++)
				{
					var jobject = JObject.Parse(jarray[k].ToString());
					text2 += jobject["content"].ToString().Replace("\r", "").Replace("\n", "");
				}
				shupai_Right_txt = shupai_Right_txt + text2 + "\r\n";
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public bool split_paragraph(string text)
		{
			return "。？！?!：".IndexOf(text) != -1;
		}

		public void baidu_image_a(object objEvent)
		{
			try
			{
				for (var i = 0; i < image_num[0]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_A(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		public void baidu_image_b(object objEvent)
		{
			try
			{
				for (var i = image_num[0]; i < image_num[1]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_B(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		private void DoWork(object state)
		{
			var array = new ManualResetEvent[5];
			array[0] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_a, array[0]);
			array[1] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_b, array[1]);
			array[2] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_c, array[2]);
			array[3] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_d, array[3]);
			array[4] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_e, array[4]);
			WaitHandle[] waitHandles = array;
			WaitHandle.WaitAll(waitHandles);
			shupai_Right_txt = string.Concat(OCR_baidu_a, OCR_baidu_b, OCR_baidu_c, OCR_baidu_d, OCR_baidu_e).Replace("\r\n\r\n", "");
			var text = shupai_Right_txt.TrimEnd('\n').TrimEnd('\r').TrimEnd('\n');
			if (text.Split(Environment.NewLine.ToCharArray()).Length > 1)
			{
				var array2 = text.Split(new[]
				{
					"\r\n"
				}, StringSplitOptions.None);
				var str = "";
				for (var i = 0; i < array2.Length; i++)
				{
					str = str + array2[array2.Length - i - 1].Replace("\r", "").Replace("\n", "") + "\r\n";
				}
				shupai_Left_txt = str;
			}
			fmloading.fml_close = "窗体已关闭";
			Invoke(new ocr_thread(Main_OCR_Thread_last));
			try
			{
				DeleteFile("Data\\image_temp");
			}
			catch
			{
				exit_thread();
			}
			image_ori.Dispose();
		}

		public void OCR_baidu_use_B(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_b = (OCR_baidu_b + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_baidu_use_A(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_a = (OCR_baidu_a + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void DeleteFile(string path)
		{
			if (File.GetAttributes(path) == FileAttributes.Directory)
			{
				Directory.Delete(path, true);
				return;
			}
			File.Delete(path);
		}

		public void OCR_baidu_image(Image imagearr, string str_image)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				str_image = (str_image + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_baidu_use_E(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_e = (OCR_baidu_e + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_baidu_use_D(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_d = (OCR_baidu_d + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_baidu_use_C(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_c = (OCR_baidu_c + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void baidu_image_c(object objEvent)
		{
			try
			{
				for (var i = image_num[1]; i < image_num[2]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_C(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		public void baidu_image_d(object objEvent)
		{
			try
			{
				for (var i = image_num[2]; i < image_num[3]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_D(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		public void baidu_image_e(object objEvent)
		{
			try
			{
				for (var i = image_num[3]; i < image_num[4]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_E(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		public void bool_image_count(int num)
		{
			if (num >= 5)
			{
				image_num = new int[num];
				if (num - num / 5 * 5 == 0)
				{
					image_num[0] = num / 5;
					image_num[1] = num / 5 * 2;
					image_num[2] = num / 5 * 3;
					image_num[3] = num / 5 * 4;
					image_num[4] = num;
				}
				if (num - num / 5 * 5 == 1)
				{
					image_num[0] = num / 5 + 1;
					image_num[1] = num / 5 * 2;
					image_num[2] = num / 5 * 3;
					image_num[3] = num / 5 * 4;
					image_num[4] = num;
				}
				if (num - num / 5 * 5 == 2)
				{
					image_num[0] = num / 5 + 1;
					image_num[1] = num / 5 * 2 + 1;
					image_num[2] = num / 5 * 3;
					image_num[3] = num / 5 * 4;
					image_num[4] = num;
				}
				if (num - num / 5 * 5 == 3)
				{
					image_num[0] = num / 5 + 1;
					image_num[1] = num / 5 * 2 + 1;
					image_num[2] = num / 5 * 3 + 1;
					image_num[3] = num / 5 * 4;
					image_num[4] = num;
				}
				if (num - num / 5 * 5 == 4)
				{
					image_num[0] = num / 5 + 1;
					image_num[1] = num / 5 * 2 + 1;
					image_num[2] = num / 5 * 3 + 1;
					image_num[3] = num / 5 * 4 + 1;
					image_num[4] = num;
				}
			}
			if (num == 4)
			{
				image_num = new int[5];
				image_num[0] = 1;
				image_num[1] = 2;
				image_num[2] = 3;
				image_num[3] = 4;
				image_num[4] = 0;
			}
			if (num == 3)
			{
				image_num = new int[5];
				image_num[0] = 1;
				image_num[1] = 2;
				image_num[2] = 3;
				image_num[3] = 0;
				image_num[4] = 0;
			}
			if (num == 2)
			{
				image_num = new int[5];
				image_num[0] = 1;
				image_num[1] = 2;
				image_num[2] = 0;
				image_num[3] = 0;
				image_num[4] = 0;
			}
			if (num == 1)
			{
				image_num = new int[5];
				image_num[0] = 1;
				image_num[1] = 0;
				image_num[2] = 0;
				image_num[3] = 0;
				image_num[4] = 0;
			}
			if (num == 0)
			{
				image_num = new int[5];
				image_num[0] = 0;
				image_num[1] = 0;
				image_num[2] = 0;
				image_num[3] = 0;
				image_num[4] = 0;
			}
		}

		public void exit_thread()
		{
			try
			{
				StaticValue.截图排斥 = false;
				esc = "退出";
				fmloading.fml_close = "窗体已关闭";
				esc_thread.Abort();
			}
			catch
			{
			}
			FormBorderStyle = FormBorderStyle.Sizable;
			Visible = true;
			base.Show();
			WindowState = FormWindowState.Normal;
			if (IniHelp.GetValue("快捷键", "翻译文本") != "请按下快捷键")
			{
				var value = IniHelp.GetValue("快捷键", "翻译文本");
				var text = "None";
				var text2 = "F9";
				SetHotkey(text, text2, value, 205);
			}
			HelpWin32.UnregisterHotKey(Handle, 222);
		}

		private Image<Gray, byte> randon(Image<Gray, byte> imageInput)
		{
			var width = imageInput.Width;
			var height = imageInput.Height;
			var num = 0;
			var array = new int[height];
			var result = imageInput;
			for (var i = -20; i < 20; i++)
			{
				var image = imageInput.Rotate(i, new Gray(1.0));
				for (var j = 0; j < height; j++)
				{
					var num2 = 0;
					for (var k = 0; k < width; k++)
					{
						num2 += image.Data[j, k, 0];
					}
					array[j] = num2;
				}
				var num3 = 0;
				for (var l = 0; l < height - 1; l++)
				{
					num3 += Math.Abs(array[l] - array[l + 1]);
				}
				if (num3 > num)
				{
					result = image;
					num = num3;
				}
			}
			return result;
		}

		public void SetGlobalProxy()
		{
			WebRequest.DefaultWebProxy = null;
		}

		private void tray_null_Proxy_Click(object sender, EventArgs e)
		{
			null_Proxy.Text = "不使用代理√";
			customize_Proxy.Text = "自定义代理";
			system_Proxy.Text = "系统代理";
			Proxy_flag = "关闭";
			WebRequest.DefaultWebProxy = null;
		}

		private void tray_system_Proxy_Click(object sender, EventArgs e)
		{
			null_Proxy.Text = "不使用代理";
			customize_Proxy.Text = "自定义代理";
			system_Proxy.Text = "系统代理√";
			Proxy_flag = "系统";
			WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
		}

		public void change_pinyin_Click(object sender, EventArgs e)
		{
			pinyin_flag = true;
			transtalate_Click();
		}

		public string Post_Html_pinyin(string url, string post_str)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 6000;
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		private Bitmap ZoomImage(Bitmap bitmap1, int destHeight, int destWidth)
		{
			var num = (double)bitmap1.Width;
			var num2 = (double)bitmap1.Height;
			if (num < destHeight)
			{
				while (num < destHeight)
				{
					num2 *= 1.1;
					num *= 1.1;
				}
			}
			if (num2 < destWidth)
			{
				while (num2 < destWidth)
				{
					num2 *= 1.1;
					num *= 1.1;
				}
			}
			var width = (int)num;
			var height = (int)num2;
			var bitmap2 = new Bitmap(width, height);
			var graphics = Graphics.FromImage(bitmap2);
			graphics.DrawImage(bitmap1, 0, 0, width, height);
			graphics.Save();
			graphics.Dispose();
			return new Bitmap(bitmap2);
		}

		public void 翻译文本()
		{
			if (IniHelp.GetValue("配置", "快速翻译") == "True")
			{
				var data = "";
				try
				{
					trans_hotkey = GetTextFromClipboard();
					if (IniHelp.GetValue("配置", "翻译接口") == "谷歌")
					{
						data = Translate_Google(trans_hotkey);
					}
					if (IniHelp.GetValue("配置", "翻译接口") == "百度")
					{
						data = Translate_baidu(trans_hotkey);
					}
					if (IniHelp.GetValue("配置", "翻译接口") == "腾讯")
					{
						data = Translate_Tencent(trans_hotkey);
					}
					Clipboard.SetData(DataFormats.UnicodeText, data);
					SendKeys.SendWait("^v");
					return;
				}
				catch
				{
					Clipboard.SetData(DataFormats.UnicodeText, data);
					SendKeys.SendWait("^v");
					return;
				}
			}
			SendKeys.SendWait("^c");
			SendKeys.Flush();
			RichBoxBody.Text = Clipboard.GetText();
			transtalate_Click();
			FormBorderStyle = FormBorderStyle.Sizable;
			Visible = true;
			HelpWin32.SetForegroundWindow(StaticValue.mainhandle);
			base.Show();
			WindowState = FormWindowState.Normal;
			if (IniHelp.GetValue("工具栏", "顶置") == "True")
			{
				TopMost = true;
				return;
			}
			TopMost = false;
		}

		public Rectangle[] GetRects(Bitmap pic)
		{
			var list = new List<Rectangle>();
			var colors = getColors(pic);
			for (var i = 0; i < pic.Height; i++)
			{
				for (var j = 0; j < pic.Width; j++)
				{
					if (Exist(colors, i, j))
					{
						var rect = GetRect(colors, i, j);
						if (rect.Width > 10 && rect.Height > 10)
						{
							list.Add(rect);
						}
					}
				}
			}
			return list.ToArray();
		}

		public Bitmap GetRect(Image pic, Rectangle Rect)
		{
			var destRect = new Rectangle(0, 0, Rect.Width, Rect.Height);
			var bitmap = new Bitmap(destRect.Width, destRect.Height);
			var graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.FromArgb(0, 0, 0, 0));
			graphics.DrawImage(pic, destRect, Rect, GraphicsUnit.Pixel);
			graphics.Dispose();
			return bitmap;
		}

		private Bitmap[] getSubPics(Image buildPic, Rectangle[] buildRects)
		{
			var array = new Bitmap[buildRects.Length];
			for (var i = 0; i < buildRects.Length; i++)
			{
				array[i] = GetRect(buildPic, buildRects[i]);
				var filename = IniHelp.GetValue("配置", "截图位置") + "\\" + ReFileName(IniHelp.GetValue("配置", "截图位置"), "图片.Png");
				array[i].Save(filename, ImageFormat.Png);
			}
			return array;
		}

		public bool[][] getColors(Bitmap pic)
		{
			var array = new bool[pic.Height][];
			for (var i = 0; i < pic.Height; i++)
			{
				array[i] = new bool[pic.Width];
				for (var j = 0; j < pic.Width; j++)
				{
					var pixel = pic.GetPixel(j, i);
					var num = 0;
					if (pixel.R < 4)
					{
						num++;
					}
					if (pixel.G < 4)
					{
						num++;
					}
					if (pixel.B < 4)
					{
						num++;
					}
					if (pixel.A < 3 || (num >= 2 && pixel.A < 30))
					{
						array[i][j] = false;
					}
					else
					{
						array[i][j] = true;
					}
				}
			}
			return array;
		}

		public bool Exist(bool[][] Colors, int x, int y)
		{
			return x >= 0 && y >= 0 && x < Colors.Length && y < Colors[0].Length && Colors[x][y];
		}

		public bool R_Exist(bool[][] Colors, Rectangle Rect)
		{
			if (Rect.Right >= Colors[0].Length || Rect.Left < 0)
			{
				return false;
			}
			for (var i = 0; i < Rect.Height; i++)
			{
				if (Exist(Colors, Rect.Top + i, Rect.Right + 1))
				{
					return true;
				}
			}
			return false;
		}

		public bool D_Exist(bool[][] Colors, Rectangle Rect)
		{
			if (Rect.Bottom >= Colors.Length || Rect.Top < 0)
			{
				return false;
			}
			for (var i = 0; i < Rect.Width; i++)
			{
				if (Exist(Colors, Rect.Bottom + 1, Rect.Left + i))
				{
					return true;
				}
			}
			return false;
		}

		public bool L_Exist(bool[][] Colors, Rectangle Rect)
		{
			if (Rect.Right >= Colors[0].Length || Rect.Left < 0)
			{
				return false;
			}
			for (var i = 0; i < Rect.Height; i++)
			{
				if (Exist(Colors, Rect.Top + i, Rect.Left - 1))
				{
					return true;
				}
			}
			return false;
		}

		public bool U_Exist(bool[][] Colors, Rectangle Rect)
		{
			if (Rect.Bottom >= Colors.Length || Rect.Top < 0)
			{
				return false;
			}
			for (var i = 0; i < Rect.Width; i++)
			{
				if (Exist(Colors, Rect.Top - 1, Rect.Left + i))
				{
					return true;
				}
			}
			return false;
		}

		public Rectangle GetRect(bool[][] Colors, int x, int y)
		{
			var rectangle = new Rectangle(new Point(y, x), new Size(1, 1));
			bool flag;
			int num;
			do
			{
				flag = false;
				while (R_Exist(Colors, rectangle))
				{
					num = rectangle.Width;
					rectangle.Width = num + 1;
					flag = true;
				}
				while (D_Exist(Colors, rectangle))
				{
					num = rectangle.Height;
					rectangle.Height = num + 1;
					flag = true;
				}
				while (L_Exist(Colors, rectangle))
				{
					num = rectangle.Width;
					rectangle.Width = num + 1;
					num = rectangle.X;
					rectangle.X = num - 1;
					flag = true;
				}
				while (U_Exist(Colors, rectangle))
				{
					num = rectangle.Height;
					rectangle.Height = num + 1;
					num = rectangle.Y;
					rectangle.Y = num - 1;
					flag = true;
				}
			}
			while (flag);
			clearRect(Colors, rectangle);
			num = rectangle.Width;
			rectangle.Width = num + 1;
			num = rectangle.Height;
			rectangle.Height = num + 1;
			return rectangle;
		}

		public void clearRect(bool[][] Colors, Rectangle Rect)
		{
			for (var i = Rect.Top; i <= Rect.Bottom; i++)
			{
				for (var j = Rect.Left; j <= Rect.Right; j++)
				{
					Colors[i][j] = false;
				}
			}
		}

		public static string ReFileNamekey(string strFilePath)
		{
			var startIndex = strFilePath.LastIndexOf('.');
			var format = strFilePath.Insert(startIndex, "_{0}");
			var num = 1;
			var text = string.Format(format, num);
			while (File.Exists(text))
			{
				text = string.Format(format, num);
				num++;
			}
			return text;
		}

		private Bitmap[] getSubPics_ocr(Image buildPic, Rectangle[] buildRects)
		{
			var text = "";
			var array = new Bitmap[buildRects.Length];
			var text2 = "";
			for (var i = 0; i < buildRects.Length; i++)
			{
				array[i] = GetRect(buildPic, buildRects[i]);
				image_screen = array[i];
				var messageload = new Messageload();
				messageload.ShowDialog();
				if (messageload.DialogResult == DialogResult.OK)
				{
					if (interface_flag == "搜狗")
					{
						OCR_sougou2();
					}
					if (interface_flag == "腾讯")
					{
						OCR_Tencent();
					}
					if (interface_flag == "有道")
					{
						OCR_youdao();
					}
					if (interface_flag == "日语" || interface_flag == "中英" || interface_flag == "韩语")
					{
						OCR_baidu();
					}
					messageload.Dispose();
				}
				if (IniHelp.GetValue("工具栏", "分栏") == "True")
				{
					if (paragraph)
					{
						text = text + "\r\n" + typeset_txt.Trim();
						text2 = text2 + "\r\n" + split_txt.Trim() + "\r\n";
					}
					else
					{
						text += typeset_txt.Trim();
						text2 = text2 + "\r\n" + split_txt.Trim() + "\r\n";
					}
				}
				else if (paragraph)
				{
					text = text + "\r\n" + typeset_txt.Trim() + "\r\n";
					text2 = text2 + "\r\n" + split_txt.Trim() + "\r\n";
				}
				else
				{
					text = text + typeset_txt.Trim() + "\r\n";
					text2 = text2 + "\r\n" + split_txt.Trim() + "\r\n";
				}
			}
			typeset_txt = text.Replace("\r\n\r\n", "\r\n");
			split_txt = text2.Replace("\r\n\r\n", "\r\n");
			fmloading.fml_close = "窗体已关闭";
			Invoke(new ocr_thread(Main_OCR_Thread_last));
			return array;
		}

		public void OCR_sougou_bat(Bitmap image_screen)
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundary8orYTmcj8BHvQpVU";
				Image image = ZoomImage(image_screen, 120, 120);
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"pic\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ocr.shouji.sogou.com/v2/ocr/json");
				httpWebRequest.Timeout = 8000;
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(array, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["result"].ToString());
				checked_txt(jarray, 2, "content");
				image.Dispose();
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public Image BoundingBox_fences(Image<Gray, byte> src, Image<Bgr, byte> draw)
		{
			Image result;
			using (var vectorOfVectorOfPoint = new VectorOfVectorOfPoint())
			{
				CvInvoke.FindContours(src, vectorOfVectorOfPoint, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, default(Point));
				Image image = draw.ToBitmap();
				var graphics = Graphics.FromImage(image);
				var size = vectorOfVectorOfPoint.Size;
				for (var i = 0; i < size; i++)
				{
					using (var vectorOfPoint = vectorOfVectorOfPoint[i])
					{
						var rectangle = CvInvoke.BoundingRectangle(vectorOfPoint);
						var x = rectangle.Location.X;
						var y = rectangle.Location.Y;
						var width = rectangle.Size.Width;
						var height = rectangle.Size.Height;
						graphics.FillRectangle(Brushes.White, x, 0, width, draw.Height);
					}
				}
				graphics.Dispose();
				var bitmap = new Bitmap(image.Width + 2, image.Height + 2);
				var graphics2 = Graphics.FromImage(bitmap);
				graphics2.DrawImage(image, 1, 1, image.Width, image.Height);
				graphics2.Save();
				graphics2.Dispose();
				image.Dispose();
				src.Dispose();
				result = bitmap;
			}
			return result;
		}

		public Image FindBundingBox_fences(Bitmap bitmap)
		{
			var image = new Image<Bgr, byte>(bitmap);
			var image2 = new Image<Gray, byte>(image.Width, image.Height);
			CvInvoke.CvtColor(image, image2, ColorConversion.Bgra2Gray, 0);
			var structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(6, 20), new Point(1, 1));
			CvInvoke.Erode(image2, image2, structuringElement, new Point(0, 2), 1, BorderType.Reflect101, default(MCvScalar));
			CvInvoke.Threshold(image2, image2, 100.0, 255.0, (ThresholdType)9);
			var image3 = new Image<Gray, byte>(image2.ToBitmap());
			var draw = image3.Convert<Bgr, byte>();
			var image4 = image3.Clone();
			CvInvoke.Canny(image3, image4, 255.0, 255.0, 5, true);
			var image5 = BoundingBox_fences(image4, draw);
			var image6 = new Image<Gray, byte>((Bitmap)image5);
			BoundingBox_fences_Up(image6);
			image.Dispose();
			image2.Dispose();
			image3.Dispose();
			image6.Dispose();
			return image5;
		}

		public void BoundingBox_fences_Up(Image<Gray, byte> src)
		{
			using (var vectorOfVectorOfPoint = new VectorOfVectorOfPoint())
			{
				CvInvoke.FindContours(src, vectorOfVectorOfPoint, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, default(Point));
				var size = vectorOfVectorOfPoint.Size;
				var array = new Rectangle[size];
				for (var i = 0; i < size; i++)
				{
					using (var vectorOfPoint = vectorOfVectorOfPoint[i])
					{
						array[size - 1 - i] = CvInvoke.BoundingRectangle(vectorOfPoint);
					}
				}
				getSubPics_ocr(image_screen, array);
			}
		}

		public void checked_location_txt(JArray jarray, int lastlength, string words)
		{
			var num = 0;
			for (var i = 0; i < jarray.Count; i++)
			{
				var length = JObject.Parse(jarray[i].ToString())[words].ToString().Length;
				if (length > num)
				{
					num = length;
				}
			}
			var str = "";
			var text = "";
			for (var j = 0; j < jarray.Count - 1; j++)
			{
				var jobject = JObject.Parse(jarray[j].ToString());
				var array = jobject[words].ToString().ToCharArray();
				var jobject2 = JObject.Parse(jarray[j + 1].ToString());
				var array2 = jobject2[words].ToString().ToCharArray();
				var length2 = jobject[words].ToString().Length;
				var length3 = jobject2[words].ToString().Length;
				if (Math.Abs(length2 - length3) <= 0)
				{
					if (split_paragraph(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else
					{
						text += jobject[words].ToString().Trim();
					}
				}
				else if (split_paragraph(array[array.Length - lastlength].ToString()) && Math.Abs(length2 - length3) <= 1)
				{
					if (split_paragraph(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else
					{
						text += jobject[words].ToString().Trim();
					}
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && length2 <= num / 2)
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()) && length3 - length2 < 4 && array2[1].ToString() == ".")
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_en(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text = text + jobject[words].ToString().Trim() + " ";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_en(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (Is_punctuation(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (Is_punctuation(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text = text + jobject[words].ToString().Trim() + " ";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (IsNum(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (IsNum(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				if (has_punctuation(jobject[words].ToString()))
				{
					text += "\r\n";
				}
				str = str + jobject[words].ToString().Trim() + "\r\n";
			}
			split_txt = str + JObject.Parse(jarray[jarray.Count - 1].ToString())[words];
			typeset_txt = text.Replace("\r\n\r\n", "\r\n") + JObject.Parse(jarray[jarray.Count - 1].ToString())[words];
		}

		public void checked_location_sougou(JArray jarray, int lastlength, string words, string location)
		{
			paragraph = false;
			var num = 20000;
			var num2 = 0;
			for (var i = 0; i < jarray.Count; i++)
			{
				var jobject = JObject.Parse(jarray[i].ToString());
				var num3 = split_char_x(jobject[location][1].ToString()) - split_char_x(jobject[location][0].ToString());
				if (num3 > num2)
				{
					num2 = num3;
				}
				var num4 = split_char_x(jobject[location][0].ToString());
				if (num4 < num)
				{
					num = num4;
				}
			}
			var jobject2 = JObject.Parse(jarray[0].ToString());
			if (Math.Abs(split_char_x(jobject2[location][0].ToString()) - num) > 10)
			{
				paragraph = true;
			}
			var text = "";
			var text2 = "";
			for (var j = 0; j < jarray.Count; j++)
			{
				var jobject3 = JObject.Parse(jarray[j].ToString());
				var array = jobject3[words].ToString().ToCharArray();
				var jobject4 = JObject.Parse(jarray[j].ToString());
				var flag = Math.Abs(split_char_x(jobject4[location][1].ToString()) - split_char_x(jobject4[location][0].ToString()) - num2) > 20;
				var flag2 = Math.Abs(split_char_x(jobject4[location][0].ToString()) - num) > 10;
				if (flag && flag2)
				{
					text = text.Trim() + "\r\n" + jobject4[words].ToString().Trim();
				}
				else if (IsNum(array[0].ToString()) && !contain_ch(array[1].ToString()) && flag)
				{
					text = text.Trim() + "\r\n" + jobject4[words].ToString().Trim() + "\r\n";
				}
				else
				{
					text += jobject4[words].ToString().Trim();
				}
				if (contain_en(array[array.Length - lastlength].ToString()))
				{
					text = text + jobject3[words].ToString().Trim() + " ";
				}
				text2 = text2 + jobject4[words].ToString().Trim() + "\r\n";
			}
			split_txt = text2.Replace("\r\n\r\n", "\r\n");
			typeset_txt = text;
		}

		public int split_char_x(string split_char)
		{
			return Convert.ToInt32(split_char.Split(',')[0]);
		}

		private void tray_double_Click(object sender, EventArgs e)
		{
			HelpWin32.UnregisterHotKey(Handle, 205);
			menu.Hide();
			RichBoxBody.Hide = "";
			RichBoxBody_T.Hide = "";
			Main_OCR_Quickscreenshots();
		}

		public int en_count(string text)
		{
			return Regex.Matches(text, "\\s+").Count + 1;
		}

		public int ch_count(string str)
		{
			var num = 0;
			var regex = new Regex("^[\\u4E00-\\u9FA5]{0,}$");
			for (var i = 0; i < str.Length; i++)
			{
				if (regex.IsMatch(str[i].ToString()))
				{
					num++;
				}
			}
			return num;
		}

		public void checked_location_youdao(JArray jarray, int lastlength, string words, string location)
		{
			paragraph = false;
			var num = 20000;
			var num2 = 0;
			for (var i = 0; i < jarray.Count; i++)
			{
				var jobject = JObject.Parse(jarray[i].ToString());
				var num3 = split_char_youdao(jobject[location].ToString(), 3) - split_char_youdao(jobject[location].ToString(), 1);
				if (num3 > num2)
				{
					num2 = num3;
				}
				var num4 = split_char_youdao(jobject[location].ToString(), 1);
				if (num4 < num)
				{
					num = num4;
				}
			}
			var jobject2 = JObject.Parse(jarray[0].ToString());
			if (Math.Abs(split_char_youdao(jobject2[location].ToString(), 1) - num) > 10)
			{
				paragraph = true;
			}
			var text = "";
			var text2 = "";
			for (var j = 0; j < jarray.Count; j++)
			{
				var jobject3 = JObject.Parse(jarray[j].ToString());
				var array = jobject3[words].ToString().ToCharArray();
				var jobject4 = JObject.Parse(jarray[j].ToString());
				var flag = Math.Abs(split_char_youdao(jobject4[location].ToString(), 3) - split_char_youdao(jobject4[location].ToString(), 1) - num2) > 20;
				var flag2 = Math.Abs(split_char_youdao(jobject4[location].ToString(), 1) - num) > 10;
				if (flag && flag2)
				{
					text = text.Trim() + "\r\n" + jobject4[words].ToString().Trim();
				}
				else if (IsNum(array[0].ToString()) && !contain_ch(array[1].ToString()) && flag)
				{
					text = text.Trim() + "\r\n" + jobject4[words].ToString().Trim() + "\r\n";
				}
				else
				{
					text += jobject4[words].ToString().Trim();
				}
				if (contain_en(array[array.Length - lastlength].ToString()))
				{
					text = text + jobject3[words].ToString().Trim() + " ";
				}
				text2 = text2 + jobject4[words].ToString().Trim() + "\r\n";
			}
			split_txt = text2.Replace("\r\n\r\n", "\r\n");
			typeset_txt = text;
		}

		public int split_char_youdao(string split_char, int i)
		{
			return Convert.ToInt32(split_char.Split(',')[i - 1]);
		}

		public void Trans_google_Click(object sender, EventArgs e)
		{
			Trans_foreach("谷歌");
		}

		public void Trans_baidu_Click(object sender, EventArgs e)
		{
			Trans_foreach("百度");
		}

		private void Trans_foreach(string name)
		{
			if (name == "百度")
			{
				trans_baidu.Text = "百度√";
				trans_google.Text = "谷歌";
				trans_tencent.Text = "腾讯";
				IniHelp.SetValue("配置", "翻译接口", "百度");
			}
			if (name == "谷歌")
			{
				trans_baidu.Text = "百度";
				trans_google.Text = "谷歌√";
				trans_tencent.Text = "腾讯";
				IniHelp.SetValue("配置", "翻译接口", "谷歌");
			}
			if (name == "腾讯")
			{
				trans_google.Text = "谷歌";
				trans_baidu.Text = "百度";
				trans_tencent.Text = "腾讯√";
				IniHelp.SetValue("配置", "翻译接口", "腾讯");
			}
		}

		public string GetBaiduHtml(string url, CookieContainer cookie, string refer, string content_length)
		{
			string result;
			try
			{
				var text = "";
				var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = refer;
				httpWebRequest.Timeout = 1500;
				httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				var bytes = Encoding.UTF8.GetBytes(content_length);
				var requestStream = httpWebRequest.GetRequestStream();
				requestStream.Write(bytes, 0, bytes.Length);
				requestStream.Close();
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = GetBaiduHtml(url, cookie, refer, content_length);
			}
			return result;
		}

		private string Translate_baidu(string Text)
		{
			var text = "";
			try
			{
				new CookieContainer();
				var text2 = "zh";
				var text3 = "en";
				if (StaticValue.zh_en)
				{
					if (ch_count(Text.Trim()) > en_count(Text.Trim()) || (en_count(text.Trim()) == 1 && ch_count(text.Trim()) == 1))
					{
						text2 = "zh";
						text3 = "en";
					}
					else
					{
						text2 = "en";
						text3 = "zh";
					}
				}
				if (StaticValue.zh_jp)
				{
					if (contain_jap(repalceStr(Del_ch(Text.Trim()))))
					{
						text2 = "jp";
						text3 = "zh";
					}
					else
					{
						text2 = "zh";
						text3 = "jp";
					}
				}
				if (StaticValue.zh_ko)
				{
					if (contain_kor(Text.Trim()))
					{
						text2 = "kor";
						text3 = "zh";
					}
					else
					{
						text2 = "zh";
						text3 = "kor";
					}
				}
				var httpHelper = new HttpHelper();
				var item = new HttpItem
				{
					URL = "https://fanyi.baidu.com/basetrans",
					Method = "post",
					ContentType = "application/x-www-form-urlencoded; charset=UTF-8",
					Postdata = string.Concat("query=", HttpUtility.UrlEncode(Text.Trim()).Replace("+", "%20"), "&from=", text2, "&to=", text3),
					UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Mobile Safari/537.36"
				};
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(httpHelper.GetHtml(item).Html))["trans"].ToString());
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text = text + jobject["dst"] + "\r\n";
				}
			}
			catch (Exception)
			{
				text = "[百度接口报错]：\r\n1.接口请求出现问题等待修复。";
			}
			return text;
		}

		public void Trans_tencent_Click(object sender, EventArgs e)
		{
			Trans_foreach("腾讯");
		}

		public string Content_Length(string text, string fromlang, string tolang)
		{
			return string.Concat("&source=", fromlang, "&target=", tolang, "&sourceText=", HttpUtility.UrlEncode(text).Replace("+", "%20"));
		}

		public string TencentPOST(string url, string content)
		{
			string result;
			try
			{
				var text = "";
				var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "https://fanyi.qq.com/";
				httpWebRequest.Timeout = 5000;
				httpWebRequest.Accept = "application/json, text/javascript, */*; q=0.01";
				httpWebRequest.Headers.Add("X-Requested-With: XMLHttpRequest");
				httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest.Headers.Add("Accept-Language: zh-CN,zh;q=0.9");
				httpWebRequest.Headers.Add("cookie:" + GetCookies("http://fanyi.qq.com"));
				var bytes = Encoding.UTF8.GetBytes(content);
				httpWebRequest.ContentLength = bytes.Length;
				var requestStream = httpWebRequest.GetRequestStream();
				requestStream.Write(bytes, 0, bytes.Length);
				requestStream.Close();
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
				if (text.Contains("\"records\":[]"))
				{
					Thread.Sleep(8);
					return TencentPOST(url, content);
				}
			}
			catch
			{
				result = "[腾讯接口报错]：\r\n请切换其它接口或再次尝试。";
			}
			return result;
		}

		private string Translate_Tencent(string strtrans)
		{
			var text = "";
			try
			{
				var fromlang = "zh";
				var tolang = "en";
				if (StaticValue.zh_en)
				{
					if (ch_count(strtrans.Trim()) > en_count(strtrans.Trim()) || (en_count(text.Trim()) == 1 && ch_count(text.Trim()) == 1))
					{
						fromlang = "zh";
						tolang = "en";
					}
					else
					{
						fromlang = "en";
						tolang = "zh";
					}
				}
				if (StaticValue.zh_jp)
				{
					if (contain_jap(repalceStr(Del_ch(strtrans.Trim()))))
					{
						fromlang = "jp";
						tolang = "zh";
					}
					else
					{
						fromlang = "zh";
						tolang = "jp";
					}
				}
				if (StaticValue.zh_ko)
				{
					if (contain_kor(strtrans.Trim()))
					{
						fromlang = "kr";
						tolang = "zh";
					}
					else
					{
						fromlang = "zh";
						tolang = "kr";
					}
				}
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(TencentPOST("https://fanyi.qq.com/api/translate", Content_Length(strtrans, fromlang, tolang))))["translate"]["records"].ToString());
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["targetText"].ToString();
				}
			}
			catch (Exception)
			{
				text = "[腾讯接口报错]：\r\n1.接口请求出现问题等待修复。";
			}
			return text;
		}

		public void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
		}

		private static string GetCookies(string url)
		{
			var num = 1024u;
			var stringBuilder = new StringBuilder((int)num);
			if (!InternetGetCookieEx(url, null, stringBuilder, ref num, 8192, IntPtr.Zero))
			{
				if (num < 0u)
				{
					return null;
				}
				stringBuilder = new StringBuilder((int)num);
				if (!InternetGetCookieEx(url, null, stringBuilder, ref num, 8192, IntPtr.Zero))
				{
					return null;
				}
			}
			return stringBuilder.ToString();
		}

		[DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData, int dwFlags, IntPtr lpReserved);

		public string Post_GoogletHtml(string post_str)
		{
			var result = "";
			var requestUriString = "https://translate.google.cn/translate_a/single";
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var httpWebRequest = WebRequest.Create(requestUriString) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 5000;
			httpWebRequest.Host = "translate.google.cn";
			httpWebRequest.Accept = "*/*";
			httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		public void httpDownload(string URL, string filename)
		{
			try
			{
				var httpWebResponse = (HttpWebResponse)((HttpWebRequest)WebRequest.Create(URL)).GetResponse();
				var contentLength = httpWebResponse.ContentLength;
				var responseStream = httpWebResponse.GetResponseStream();
				Stream stream = new FileStream(filename, FileMode.Create);
				var num = 0L;
				var array = new byte[2048];
				for (var i = responseStream.Read(array, 0, array.Length); i > 0; i = responseStream.Read(array, 0, array.Length))
				{
					num = i + num;
					stream.Write(array, 0, i);
				}
				stream.Close();
				responseStream.Close();
			}
			catch (Exception)
			{
				throw;
			}
		}

		public void OCR_baidu_table()
		{
			typeset_txt = "[消息]：表格已下载！";
			split_txt = "";
			try
			{
				baidu_vip = Get_html(string.Format("{0}?{1}", "https://aip.baidubce.com/oauth/2.0/token", "grant_type=client_credentials&client_id=" + StaticValue.baiduAPI_ID + "&client_secret=" + StaticValue.baiduAPI_key));
				if (baidu_vip == "")
				{
					MessageBox.Show("请检查密钥输入是否正确！", "提醒");
				}
				else
				{
					split_txt = "";
					var image = image_screen;
					var memoryStream = new MemoryStream();
					image.Save(memoryStream, ImageFormat.Jpeg);
					var array = new byte[memoryStream.Length];
					memoryStream.Position = 0L;
					memoryStream.Read(array, 0, (int)memoryStream.Length);
					memoryStream.Close();
					var s = "image=" + HttpUtility.UrlEncode(Convert.ToBase64String(array));
					var bytes = Encoding.UTF8.GetBytes(s);
					var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://aip.baidubce.com/rest/2.0/solution/v1/form_ocr/request?access_token=" + ((JObject)JsonConvert.DeserializeObject(baidu_vip))["access_token"]);
					httpWebRequest.Proxy = null;
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "application/x-www-form-urlencoded";
					httpWebRequest.Timeout = 8000;
					httpWebRequest.ReadWriteTimeout = 5000;
					using (var requestStream = httpWebRequest.GetRequestStream())
					{
						requestStream.Write(bytes, 0, bytes.Length);
					}
					var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
					var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
					responseStream.Close();
					var post_str = "request_id=" + JObject.Parse(JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["result"].ToString())[0].ToString())["request_id"].ToString().Trim() + "&result_type=json";
					var text = "";
					while (!text.Contains("已完成"))
					{
						if (text.Contains("image recognize error"))
						{
							RichBoxBody.Text = "[消息]：未发现表格！";
							break;
						}
						Thread.Sleep(120);
						text = Post_Html("https://aip.baidubce.com/rest/2.0/solution/v1/form_ocr/get_request_result?access_token=" + ((JObject)JsonConvert.DeserializeObject(baidu_vip))["access_token"], post_str);
					}
					if (!text.Contains("image recognize error"))
					{
						get_table(text);
					}
				}
			}
			catch
			{
				RichBoxBody.Text = "[消息]：免费百度密钥50次已经耗完！请更换自己的密钥继续使用！";
			}
		}

		public void OCR_table_Click(object sender, EventArgs e)
		{
			OCR_foreach("表格");
		}

		private void get_table(string str)
		{
			var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(((JObject)JsonConvert.DeserializeObject(str))["result"]["result_data"].ToString().Replace("\\", "")))["forms"][0]["body"].ToString());
			var array = new int[jarray.Count];
			var array2 = new int[jarray.Count];
			for (var i = 0; i < jarray.Count; i++)
			{
				var jobject = JObject.Parse(jarray[i].ToString());
				var value = jobject["column"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				var value2 = jobject["row"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				array[i] = Convert.ToInt32(value);
				array2[i] = Convert.ToInt32(value2);
			}
			var array3 = new string[array2.Max() + 1, array.Max() + 1];
			for (var j = 0; j < jarray.Count; j++)
			{
				var jobject2 = JObject.Parse(jarray[j].ToString());
				var value3 = jobject2["column"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				var value4 = jobject2["row"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				array[j] = Convert.ToInt32(value3);
				array2[j] = Convert.ToInt32(value4);
				var text = jobject2["word"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				array3[Convert.ToInt32(value4), Convert.ToInt32(value3)] = text;
			}
			var graphics = CreateGraphics();
			var array4 = new int[array.Max() + 1];
			var num = 0;
			var sizeF = new SizeF(10f, 10f);
			var num2 = Screen.PrimaryScreen.Bounds.Width / 4;
			for (var k = 0; k < array3.GetLength(1); k++)
			{
				for (var l = 0; l < array3.GetLength(0); l++)
				{
					sizeF = graphics.MeasureString(array3[l, k], new Font("宋体", 12f));
					if (num < (int)sizeF.Width)
					{
						num = (int)sizeF.Width;
					}
					if (num > num2)
					{
						num = num2;
					}
				}
				array4[k] = num;
				num = 0;
			}
			graphics.Dispose();
			setClipboard_Table(array3, array4);
		}

		private void setClipboard_table(string[,] wordo)
		{
			var str = "{\\rtf1\\ansi\\ansicpg936\\deff0\\deflang1033\\deflangfe2052{\\fonttbl{\\f0\\fnil\\fprq2\\fcharset134";
			str += "\\'cb\\'ce\\'cc\\'e5;}{\\f1\\fnil\\fcharset134 \\'cb\\'ce\\'cc\\'e5;}}\\viewkind4\\uc1\\trowd\\trgaph108\\trleft-108";
			str += "\\trbrdrt\\brdrs\\brdrw10 \\trbrdrl\\brdrs\\brdrw10 \\trbrdrb\\brdrs\\brdrw10 \\trbrdrb\\brdrs\\brdrw10 ";
			for (var i = 1; i <= wordo.GetLength(1); i++)
			{
				str = str + "\\clbrdrt\\brdrw15\\brdrs\\clbrdrl\\brdrw15\\brdrs\\clbrdrb\\brdrw15\\brdrs\\clbrdrr\\brdrw15\\brdrs \\cellx" + (i * 1800);
			}
			var text = "";
			var str2 = "\\pard\\intbl\\kerning2\\f0";
			var str3 = "\\row\\pard\\lang2052\\kerning0\\f1\\fs18\\par}";
			for (var j = 0; j < wordo.GetLength(0); j++)
			{
				for (var k = 0; k < wordo.GetLength(1); k++)
				{
					if (k == 0)
					{
						text = text + "\\fs24 " + wordo[j, k];
					}
					else
					{
						text = text + "\\cell " + wordo[j, k];
					}
				}
				if (j != wordo.GetLength(0) - 1)
				{
					text += "\\row\\intbl";
				}
			}
			RichBoxBody.rtf = str + str2 + text + str3;
		}

		public void Main_OCR_Thread_table()
		{
			ailibaba = new AliTable();
			var timeSpan = new TimeSpan(DateTime.Now.Ticks);
			var timeSpan2 = timeSpan.Subtract(ts).Duration();
			var str = string.Concat(new[]
			{
				timeSpan2.Seconds.ToString(),
				".",
				Convert.ToInt32(timeSpan2.TotalMilliseconds).ToString(),
				"秒"
			});
			if (StaticValue.v_topmost)
			{
				TopMost = true;
			}
			else
			{
				TopMost = false;
			}
			Text = "耗时：" + str;
			if (interface_flag == "百度表格")
			{
				var dataObject = new DataObject();
				dataObject.SetData(DataFormats.Rtf, RichBoxBody.rtf);
				dataObject.SetData(DataFormats.UnicodeText, RichBoxBody.Text);
				RichBoxBody.Text = "[消息]：表格已复制到粘贴板！";
				Clipboard.SetDataObject(dataObject);
			}
			image_screen.Dispose();
			GC.Collect();
			StaticValue.截图排斥 = false;
			FormBorderStyle = FormBorderStyle.Sizable;
			Visible = true;
			base.Show();
			WindowState = FormWindowState.Normal;
			Size = new Size(form_width, form_height);
			HelpWin32.SetForegroundWindow(Handle);
			if (interface_flag == "阿里表格")
			{
				if (split_txt == "弹出cookie")
				{
					split_txt = "";
					ailibaba.TopMost = true;
					ailibaba.getcookie = "";
					IniHelp.SetValue("特殊", "ali_cookie", ailibaba.getcookie);
					ailibaba.ShowDialog();
					HelpWin32.SetForegroundWindow(ailibaba.Handle);
					return;
				}
				Clipboard.SetDataObject(typeset_txt);
				CopyHtmlToClipBoard(typeset_txt);
			}
		}

		private void setClipboard_Table(string[,] wordo, int[] cc)
		{
			var str = "{\\rtf1\\ansi\\ansicpg936\\deff0\\deflang1033\\deflangfe2052{\\fonttbl{\\f0\\fnil\\fprq2\\fcharset134";
			str += "\\'cb\\'ce\\'cc\\'e5;}{\\f1\\fnil\\fcharset134 \\'cb\\'ce\\'cc\\'e5;}}\\viewkind4\\uc1\\trowd\\trgaph108\\trleft-108";
			str += "\\trbrdrt\\brdrs\\brdrw10 \\trbrdrl\\brdrs\\brdrw10 \\trbrdrb\\brdrs\\brdrw10 \\trbrdrb\\brdrs\\brdrw10 ";
			var num = 0;
			for (var i = 1; i <= cc.Length; i++)
			{
				num += cc[i - 1] * 17;
				str = str + "\\clbrdrt\\brdrw15\\brdrs\\clbrdrl\\brdrw15\\brdrs\\clbrdrb\\brdrw15\\brdrs\\clbrdrr\\brdrw15\\brdrs \\cellx" + num;
			}
			var text = "";
			var str2 = "\\pard\\intbl\\kerning2\\f0";
			var str3 = "\\row\\pard\\lang2052\\kerning0\\f1\\fs18\\par}";
			for (var j = 0; j < wordo.GetLength(0); j++)
			{
				for (var k = 0; k < wordo.GetLength(1); k++)
				{
					if (k == 0)
					{
						text = text + "\\fs24 " + wordo[j, k];
					}
					else
					{
						text = text + "\\cell " + wordo[j, k];
					}
				}
				if (j != wordo.GetLength(0) - 1)
				{
					text += "\\row\\intbl";
				}
			}
			RichBoxBody.rtf = str + str2 + text + str3;
		}

		public string Translate_Googlekey(string text)
		{
			var text2 = "";
			try
			{
				var text3 = "zh-CN";
				var text4 = "en";
				if (StaticValue.zh_en)
				{
					if (ch_count(typeset_txt.Trim()) > en_count(typeset_txt.Trim()))
					{
						text3 = "zh-CN";
						text4 = "en";
					}
					else
					{
						text3 = "en";
						text4 = "zh-CN";
					}
				}
				if (StaticValue.zh_jp)
				{
					if (contain_jap(repalceStr(Del_ch(typeset_txt.Trim()))))
					{
						text3 = "ja";
						text4 = "zh-CN";
					}
					else
					{
						text3 = "zh-CN";
						text4 = "ja";
					}
				}
				if (StaticValue.zh_ko)
				{
					if (contain_kor(typeset_txt.Trim()))
					{
						text3 = "ko";
						text4 = "zh-CN";
					}
					else
					{
						text3 = "zh-CN";
						text4 = "ko";
					}
				}
				var post_str = string.Concat("client=gtx&sl=", text3, "&tl=", text4, "&dt=t&q=", HttpUtility.UrlEncode(text).Replace("+", "%20"));
				var jarray = (JArray)JsonConvert.DeserializeObject(Post_GoogletHtml(post_str));
				var count = ((JArray)jarray[0]).Count;
				for (var i = 0; i < count; i++)
				{
					text2 += jarray[0][i][0].ToString();
				}
			}
			catch (Exception)
			{
				text2 = "[谷歌接口报错]：\r\n出现这个提示文字，表示您当前的网络不适合使用谷歌接口，使用方法开启设置中的系统代理，看是否可行，仍不可行的话，请自行挂VPN，多的不再说，这个问题不要再和我反馈了，个人能力有限解决不了。\r\n请放弃使用谷歌接口，腾讯，百度接口都可以正常使用。";
			}
			return text2;
		}

		public void OCR_baidutable_Click(object sender, EventArgs e)
		{
			OCR_foreach("百度表格");
		}

		public void OCR_ailitable_Click(object sender, EventArgs e)
		{
			OCR_foreach("阿里表格");
		}

		private new void Refresh()
		{
			sougou.Text = "搜狗";
			tencent.Text = "腾讯";
			baidu.Text = "百度";
			youdao.Text = "有道";
			shupai.Text = "竖排";
			ocr_table.Text = "表格";
			ch_en.Text = "中英";
			jap.Text = "日语";
			kor.Text = "韩语";
			left_right.Text = "从左向右";
			righ_left.Text = "从右向左";
			baidu_table.Text = "百度";
			ali_table.Text = "阿里";
			Mathfuntion.Text = "公式";
		}

		public static byte[] ImageToByteArray(Image img)
		{
			return (byte[])new ImageConverter().ConvertTo(img, typeof(byte[]));
		}

		public static Stream BytesToStream(byte[] bytes)
		{
			return new MemoryStream(bytes);
		}

		public void OCR_ali_table()
		{
			var text = "";
			split_txt = "";
			try
			{
				var value = IniHelp.GetValue("特殊", "ali_cookie");
				var stream = BytesToStream(ImageToByteArray(BWPic((Bitmap)image_screen)));
				var str = Convert.ToBase64String(new BinaryReader(stream).ReadBytes(Convert.ToInt32(stream.Length)));
				stream.Close();
				var post_str = "{\n\t\"image\": \"" + str + "\",\n\t\"configure\": \"{\\\"format\\\":\\\"html\\\", \\\"finance\\\":false}\"\n}";
				var url = "https://predict-pai.data.aliyun.com/dp_experience_mall/ocr/ocr_table_parse";
				text = Post_Html_final(url, post_str, value);
				typeset_txt = ((JObject)JsonConvert.DeserializeObject(Post_Html_final(url, post_str, value)))["tables"].ToString().Replace("table tr td { border: 1px solid blue }", "table tr td {border: 0.5px black solid }").Replace("table { border: 1px solid blue }", "table { border: 0.5px black solid; border-collapse : collapse}\r\n");
				RichBoxBody.Text = "[消息]：表格已复制到粘贴板！";
			}
			catch
			{
				RichBoxBody.Text = "[消息]：阿里表格识别出错！";
				if (text.Contains("NEED_LOGIN"))
				{
					split_txt = "弹出cookie";
				}
			}
		}

		public Bitmap BWPic(Bitmap mybm)
		{
			var bitmap = new Bitmap(mybm.Width, mybm.Height);
			for (var i = 0; i < mybm.Width; i++)
			{
				for (var j = 0; j < mybm.Height; j++)
				{
					var pixel = mybm.GetPixel(i, j);
					var num = (pixel.R + pixel.G + pixel.B) / 3;
					bitmap.SetPixel(i, j, Color.FromArgb(num, num, num));
				}
			}
			return bitmap;
		}

		public string Post_Html_final(string url, string post_str, string CookieContainer)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Accept = "*/*";
			httpWebRequest.Timeout = 5000;
			httpWebRequest.Headers.Add("Accept-Language:zh-CN,zh;q=0.9");
			httpWebRequest.ContentType = "text/plain";
			httpWebRequest.Headers.Add("Cookie:" + CookieContainer);
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		public void CopyHtmlToClipBoard(string html)
		{
			var utf = Encoding.UTF8;
			var format = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";
			var text = "<html>\r\n<head>\r\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=" + utf.WebName + "\">\r\n<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n<!--StartFragment-->";
			var text2 = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";
			var s = string.Format(format, 0, 0, 0, 0);
			var byteCount = utf.GetByteCount(s);
			var byteCount2 = utf.GetByteCount(text);
			var byteCount3 = utf.GetByteCount(html);
			var byteCount4 = utf.GetByteCount(text2);
			var s2 = string.Format(format, byteCount, byteCount + byteCount2 + byteCount3 + byteCount4, byteCount + byteCount2, byteCount + byteCount2 + byteCount3) + text + html + text2;
			var dataObject = new DataObject();
			dataObject.SetData(DataFormats.Html, new MemoryStream(utf.GetBytes(s2)));
			var data = new HtmlToText().Convert(html);
			dataObject.SetData(DataFormats.Text, data);
			Clipboard.SetDataObject(dataObject);
		}

		public static string Encript(string functionName, object[] pams)
		{
			var code = File.ReadAllText("sign.js");
            ScriptControlClass scriptControlClass = new ScriptControlClass();
			((IScriptControl)scriptControlClass).Language = "javascript";
			((IScriptControl)scriptControlClass).AddCode(code);
			return ((IScriptControl)scriptControlClass).Run(functionName, ref pams).ToString();
		}

		private object ExecuteScript(string sExpression, string sCode)
		{
			ScriptControl scriptControl = new ScriptControlClass();
			scriptControl.UseSafeSubset = true;
			scriptControl.Language = "JScript";
			scriptControl.AddCode(sCode);
			try
			{
				return scriptControl.Eval(sExpression);
			}
			catch (Exception)
			{
			}
			return null;
		}

		private void OCR_Mathfuntion_Click(object sender, EventArgs e)
		{
			OCR_foreach("公式");
		}

		public void OCR_Math()
		{
			split_txt = "";
			try
			{
				var img = image_screen;
				var inArray = OCR_ImgToByte(img);
				var s = "{\t\"formats\": [\"latex_styled\", \"text\"],\t\"metadata\": {\t\t\"count\": 1,\t\t\"platform\": \"windows 10\",\t\t\"skip_recrop\": true,\t\t\"user_id\": \"123ab2a82ea246a0b011a37183c87bab\",\t\t\"version\": \"snip.windows@00.00.0083\"\t},\t\"ocr\": [\"text\", \"math\"],\t\"src\": \"data:image/jpeg;base64," + Convert.ToBase64String(inArray) + "\"}";
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.mathpix.com/v3/latex");
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Timeout = 8000;
				httpWebRequest.ReadWriteTimeout = 5000;
				httpWebRequest.Headers.Add("app_id: mathpix_chrome");
				httpWebRequest.Headers.Add("app_key: 85948264c5d443573286752fbe8df361");
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var text = "$" + ((JObject)JsonConvert.DeserializeObject(value))["latex_styled"] + "$";
				split_txt = text;
				typeset_txt = text;
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本或者密钥次数用尽***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public string interface_flag;

		public string language;

		public string split_txt;

		public string note;

		public string spacechar;

		public string richTextBox1_note;

		public string transtalate_fla;

		public Fmloading fmloading;

		public Thread thread;

		public MenuItem Set;

		public string googleTranslate_txt;

		public int num_ok;

		public bool bolActive;

		public bool tencent_vip_f;

		public string auto_fla;

		public string baidu_vip;

		public string htmltxt;

		public static string TipText;

		public bool speaking;

		public static bool speak_copy;

		public string speak_copyb;

		public string speak_stop;

		public byte[] ttsData;

		public string[] pubnote;

		public Fmnote fmnote;

		public Image image_screen;

		public int voice_count;

		public int form_width;

		public int form_height;

		public bool change_QQ_screenshot;

		private FmFlags fmflags;

		public string trans_hotkey;

		public TimeSpan ts;

		public System.Windows.Forms.Timer esc_timer;

		public Thread esc_thread;

		public string esc;

		private string languagle_flag;

		public static string GetTkkJS;

		public string typeset_txt;

		public string baidu_flags;

		public bool 截图排斥;

		private Image image_ori;

		public string shupai_Right_txt;

		private AutoResetEvent are;

		public string baiducookies;

		public string shupai_Left_txt;

		public Image[] image_arr;

		public string OCR_baidu_a;

		public string OCR_baidu_b;

		public List<Image> imgArr;

		public List<Image> imagelist;

		public int imagelist_lenght;

		public string OCR_baidu_d;

		public string OCR_baidu_c;

		public string OCR_baidu_e;

		public int[] image_num;

		public string Proxy_flag;

		public string Proxy_url;

		public string Proxy_port;

		public string Proxy_name;

		public string Proxy_password;

		public bool pinyin_flag;

		public bool set_split;

		public bool set_merge;

		public bool tranclick;

		public string myjsTextBox;

		private string flags_ocrorder;

		public int first_line;

		public bool paragraph;

		public WebBrowser webBrowser;

		public string tencent_cookie;

		private AliTable ailibaba;

		// (Invoke) Token: 0x06000138 RID: 312
		public delegate void translate();

		// (Invoke) Token: 0x0600013C RID: 316
		public delegate void ocr_thread();

		// (Invoke) Token: 0x06000140 RID: 320
		public delegate int Dllinput(string command);

		public class AutoClosedMsgBox
		{

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

			[DllImport("user32.dll")]
			private static extern bool EndDialog(IntPtr hDlg, int nResult);

			[DllImport("user32.dll")]
			private static extern int MessageBoxTimeout(IntPtr hwnd, string txt, string caption, int wtype, int wlange, int dwtimeout);

			public static int Show(string text, string caption, int milliseconds, MsgBoxStyle style)
			{
				return MessageBoxTimeout(IntPtr.Zero, text, caption, (int)style, 0, milliseconds);
			}

			public static int Show(string text, string caption, int milliseconds, int style)
			{
				return MessageBoxTimeout(IntPtr.Zero, text, caption, style, 0, milliseconds);
			}

			private const int WM_CLOSE = 16;
		}

		public enum MsgBoxStyle
		{

			OK,

			OKCancel,

			AbortRetryIgnore,

			YesNoCancel,

			YesNo,

			RetryCancel,

			CancelRetryContinue,

			RedCritical_OK = 16,

			RedCritical_OKCancel,

			RedCritical_AbortRetryIgnore,

			RedCritical_YesNoCancel,

			RedCritical_YesNo,

			RedCritical_RetryCancel,

			RedCritical_CancelRetryContinue,

			BlueQuestion_OK = 32,

			BlueQuestion_OKCancel,

			BlueQuestion_AbortRetryIgnore,

			BlueQuestion_YesNoCancel,

			BlueQuestion_YesNo,

			BlueQuestion_RetryCancel,

			BlueQuestion_CancelRetryContinue,

			YellowAlert_OK = 48,

			YellowAlert_OKCancel,

			YellowAlert_AbortRetryIgnore,

			YellowAlert_YesNoCancel,

			YellowAlert_YesNo,

			YellowAlert_RetryCancel,

			YellowAlert_CancelRetryContinue,

			BlueInfo_OK = 64,

			BlueInfo_OKCancel,

			BlueInfo_AbortRetryIgnore,

			BlueInfo_YesNoCancel,

			BlueInfo_YesNo,

			BlueInfo_RetryCancel,

			BlueInfo_CancelRetryContinue
		}

		private class PinYin
		{

			static PinYin()
			{
				var text = "吖,ā|阿,ā|啊,ā|锕,ā|錒,ā|嗄,á|厑,ae|哎,āi|哀,āi|唉,āi|埃,āi|挨,āi|溾,āi|锿,āi|鎄,āi|啀,ái|捱,ái|皑,ái|凒,ái|嵦,ái|溰,ái|嘊,ái|敱,ái|敳,ái|皚,ái|癌,ái|娾,ái|隑,ái|剴,ái|騃,ái|毐,ǎi|昹,ǎi|矮,ǎi|蔼,ǎi|躷,ǎi|濭,ǎi|藹,ǎi|譪,ǎi|霭,ǎi|靄,ǎi|鯦,ǎi|噯,ài|艾,ài|伌,ài|爱,ài|砹,ài|硋,ài|隘,ài|嗌,ài|塧,ài|嫒,ài|愛,ài|碍,ài|叆,ài|暧,ài|瑷,ài|僾,ài|壒,ài|嬡,ài|懓,ài|薆,ài|懝,ài|曖,ài|賹,ài|餲,ài|鴱,ài|皧,ài|瞹,ài|馤,ài|礙,ài|譺,ài|鑀,ài|鱫,ài|靉,ài|閡,ài|欬,ài|焥,ài|堨,ài|乂,ài|嗳,ài|璦,ài|安,ān|侒,ān|峖,ān|桉,ān|氨,ān|庵,ān|谙,ān|媕,ān|萻,ān|葊,ān|痷,ān|腤,ān|鹌,ān|蓭,ān|誝,ān|鞌,ān|鞍,ān|盦,ān|闇,ān|馣,ān|鮟,ān|盫,ān|鵪,ān|韽,ān|鶕,ān|啽,ān|厰,ān|鴳,ān|諳,ān|玵,án|雸,án|儑,án|垵,ǎn|俺,ǎn|唵,ǎn|埯,ǎn|铵,ǎn|隌,ǎn|揞,ǎn|晻,ǎn|罯,ǎn|銨,ǎn|碪,ǎn|犴,àn|岸,àn|按,àn|洝,àn|荌,àn|案,àn|胺,àn|豻,àn|堓,àn|婩,àn|貋,àn|錌,àn|黯,àn|頇,àn|屽,àn|垾,àn|遃,àn|暗,àn|肮,āng|骯,āng|岇,áng|昂,áng|昻,áng|卬,áng|枊,àng|盎,àng|醠,àng|凹,āo|垇,āo|柪,āo|軪,āo|爊,āo|熝,āo|眑,āo|泑,āo|梎,āo|敖,áo|厫,áo|隞,áo|嗷,áo|嗸,áo|嶅,áo|廒,áo|滶,áo|獒,áo|獓,áo|遨,áo|摮,áo|璈,áo|蔜,áo|磝,áo|翱,áo|聱,áo|螯,áo|翶,áo|謷,áo|翺,áo|鳌,áo|鏖,áo|鰲,áo|鷔,áo|鼇,áo|慠,áo|鏕,áo|嚻,áo|熬,áo|抝,ǎo|芺,ǎo|袄,ǎo|媪,ǎo|镺,ǎo|媼,ǎo|襖,ǎo|郩,ǎo|鴁,ǎo|蝹,ǎo|坳,ào|岙,ào|扷,ào|岰,ào|傲,ào|奡,ào|奥,ào|嫯,ào|奧,ào|澚,ào|墺,ào|嶴,ào|澳,ào|懊,ào|擙,ào|謸,ào|鏊,ào|驁,ào|骜,ào|吧,ba|八,bā|仈,bā|巴,bā|叭,bā|扒,bā|朳,bā|玐,bā|夿,bā|岜,bā|芭,bā|疤,bā|哵,bā|捌,bā|笆,bā|粑,bā|紦,bā|羓,bā|蚆,bā|釟,bā|鲃,bā|魞,bā|鈀,bā|柭,bā|丷,bā|峇,bā|豝,bā|叐,bá|犮,bá|抜,bá|坺,bá|妭,bá|拔,bá|茇,bá|炦,bá|癹,bá|胈,bá|釛,bá|菝,bá|詙,bá|跋,bá|軷,bá|颰,bá|魃,bá|墢,bá|鼥,bá|把,bǎ|钯,bǎ|靶,bǎ|坝,bà|弝,bà|爸,bà|罢,bà|鲅,bà|罷,bà|鮁,bà|覇,bà|矲,bà|霸,bà|壩,bà|灞,bà|欛,bà|鲌,bà|鮊,bà|皅,bà|挀,bāi|掰,bāi|白,bái|百,bǎi|佰,bǎi|柏,bǎi|栢,bǎi|捭,bǎi|竡,bǎi|粨,bǎi|絔,bǎi|摆,bǎi|擺,bǎi|襬,bǎi|庍,bài|拝,bài|败,bài|拜,bài|敗,bài|稗,bài|粺,bài|鞁,bài|薭,bài|贁,bài|韛,bài|扳,bān|攽,bān|朌,bān|班,bān|般,bān|颁,bān|斑,bān|搬,bān|斒,bān|頒,bān|瘢,bān|螁,bān|螌,bān|褩,bān|癍,bān|辬,bān|籓,bān|肦,bān|鳻,bān|搫,bān|阪,bǎn|坂,bǎn|岅,bǎn|昄,bǎn|板,bǎn|版,bǎn|钣,bǎn|粄,bǎn|舨,bǎn|鈑,bǎn|蝂,bǎn|魬,bǎn|覂,bǎn|瓪,bǎn|办,bàn|半,bàn|伴,bàn|扮,bàn|姅,bàn|怑,bàn|拌,bàn|绊,bàn|秚,bàn|湴,bàn|絆,bàn|鉡,bàn|靽,bàn|辦,bàn|瓣,bàn|跘,bàn|邦,bāng|峀,bāng|垹,bāng|帮,bāng|捠,bāng|梆,bāng|浜,bāng|邫,bāng|幚,bāng|縍,bāng|幫,bāng|鞤,bāng|幇,bāng|绑,bǎng|綁,bǎng|榜,bǎng|牓,bǎng|膀,bǎng|騯,bǎng|玤,bàng|蚌,bàng|傍,bàng|棒,bàng|棓,bàng|硥,bàng|谤,bàng|塝,bàng|徬,bàng|稖,bàng|蒡,bàng|蜯,bàng|镑,bàng|艕,bàng|謗,bàng|鎊,bàng|埲,bàng|蚄,bàng|蛖,bàng|嫎,bàng|勹,bāo|包,bāo|佨,bāo|孢,bāo|胞,bāo|剝,bāo|笣,bāo|煲,bāo|龅,bāo|蕔,bāo|褒,bāo|闁,bāo|襃,bāo|齙,bāo|剥,bāo|枹,bāo|裦,bāo|苞,bāo|窇,báo|嫑,báo|雹,báo|铇,báo|薄,báo|宝,bǎo|怉,bǎo|饱,bǎo|保,bǎo|鸨,bǎo|珤,bǎo|堡,bǎo|堢,bǎo|媬,bǎo|葆,bǎo|寚,bǎo|飹,bǎo|飽,bǎo|褓,bǎo|駂,bǎo|鳵,bǎo|緥,bǎo|賲,bǎo|藵,bǎo|寳,bǎo|寶,bǎo|靌,bǎo|宀,bǎo|鴇,bǎo|勽,bào|报,bào|抱,bào|豹,bào|菢,bào|袌,bào|報,bào|鉋,bào|鲍,bào|靤,bào|骲,bào|暴,bào|髱,bào|虣,bào|鮑,bào|儤,bào|曓,bào|爆,bào|忁,bào|鑤,bào|蚫,bào|瀑,bào|萡,be|呗,bei|唄,bei|陂,bēi|卑,bēi|盃,bēi|桮,bēi|悲,bēi|揹,bēi|碑,bēi|鹎,bēi|藣,bēi|鵯,bēi|柸,bēi|錍,bēi|椑,bēi|諀,bēi|杯,bēi|喺,béi|北,běi|鉳,běi|垻,bèi|贝,bèi|狈,bèi|貝,bèi|邶,bèi|备,bèi|昁,bèi|牬,bèi|苝,bèi|背,bèi|钡,bèi|俻,bèi|倍,bèi|悖,bèi|狽,bèi|被,bèi|偝,bèi|偹,bèi|梖,bèi|珼,bèi|備,bèi|僃,bèi|惫,bèi|焙,bèi|琲,bèi|軰,bèi|辈,bèi|愂,bèi|碚,bèi|禙,bèi|蓓,bèi|蛽,bèi|犕,bèi|褙,bèi|誖,bèi|骳,bèi|輩,bèi|鋇,bèi|憊,bèi|糒,bèi|鞴,bèi|鐾,bèi|鐴,bèi|杮,bèi|韝,bèi|棑,bèi|哱,bèi|鄁,bèi|奔,bēn|泍,bēn|贲,bēn|倴,bēn|渀,bēn|逩,bēn|犇,bēn|賁,bēn|錛,bēn|喯,bēn|锛,bēn|本,běn|苯,běn|奙,běn|畚,běn|楍,běn|翉,běn|夲,běn|坌,bèn|捹,bèn|桳,bèn|笨,bèn|撪,bèn|獖,bèn|輽,bèn|炃,bèn|燌,bèn|夯,bèn|伻,bēng|祊,bēng|奟,bēng|崩,bēng|绷,bēng|絣,bēng|閍,bēng|嵭,bēng|痭,bēng|嘣,bēng|綳,bēng|繃,bēng|嗙,bēng|挷,bēng|傰,bēng|搒,bēng|甭,béng|埄,běng|菶,běng|琣,běng|鞛,běng|琫,běng|泵,bèng|迸,bèng|逬,bèng|跰,bèng|塴,bèng|甏,bèng|镚,bèng|蹦,bèng|鏰,bèng|錋,bèng|皀,bī|屄,bī|偪,bī|毴,bī|逼,bī|豍,bī|螕,bī|鲾,bī|鎞,bī|鵖,bī|鰏,bī|悂,bī|鈚,bī|柲,bí|荸,bí|鼻,bí|嬶,bí|匕,bǐ|比,bǐ|夶,bǐ|朼,bǐ|佊,bǐ|妣,bǐ|沘,bǐ|疕,bǐ|彼,bǐ|柀,bǐ|秕,bǐ|俾,bǐ|笔,bǐ|粃,bǐ|粊,bǐ|舭,bǐ|啚,bǐ|筆,bǐ|鄙,bǐ|聛,bǐ|貏,bǐ|箄,bǐ|崥,bǐ|魮,bǐ|娝,bǐ|箃,bǐ|吡,bǐ|匂,bì|币,bì|必,bì|毕,bì|闭,bì|佖,bì|坒,bì|庇,bì|诐,bì|邲,bì|妼,bì|怭,bì|枈,bì|畀,bì|苾,bì|哔,bì|毖,bì|珌,bì|疪,bì|胇,bì|荜,bì|陛,bì|毙,bì|狴,bì|畢,bì|袐,bì|铋,bì|婢,bì|庳,bì|敝,bì|梐,bì|萆,bì|萞,bì|閇,bì|閉,bì|堛,bì|弻,bì|弼,bì|愊,bì|愎,bì|湢,bì|皕,bì|禆,bì|筚,bì|貱,bì|赑,bì|嗶,bì|彃,bì|楅,bì|滗,bì|滭,bì|煏,bì|痹,bì|痺,bì|腷,bì|蓖,bì|蓽,bì|蜌,bì|裨,bì|跸,bì|鉍,bì|閟,bì|飶,bì|幣,bì|弊,bì|熚,bì|獙,bì|碧,bì|稫,bì|箅,bì|箆,bì|綼,bì|蔽,bì|馝,bì|幤,bì|潷,bì|獘,bì|罼,bì|襅,bì|駜,bì|髲,bì|壁,bì|嬖,bì|廦,bì|篦,bì|篳,bì|縪,bì|薜,bì|觱,bì|避,bì|鮅,bì|斃,bì|濞,bì|臂,bì|蹕,bì|鞞,bì|髀,bì|奰,bì|璧,bì|鄨,bì|饆,bì|繴,bì|襞,bì|鏎,bì|鞸,bì|韠,bì|躃,bì|躄,bì|魓,bì|贔,bì|驆,bì|鷝,bì|鷩,bì|鼊,bì|咇,bì|鮩,bì|畐,bì|踾,bì|鶝,bì|闬,bì|閈,bì|祕,bì|鴓,bì|怶,bì|旇,bì|翍,bì|肶,bì|笓,bì|鸊,bì|肸,bì|畁,bì|詖,bì|鄪,bì|襣,bì|边,biān|砭,biān|笾,biān|猵,biān|编,biān|萹,biān|煸,biān|牑,biān|甂,biān|箯,biān|編,biān|蝙,biān|獱,biān|邉,biān|鍽,biān|鳊,biān|邊,biān|鞭,biān|鯿,biān|籩,biān|糄,biān|揙,biān|臱,biān|鯾,biān|炞,biǎn|贬,biǎn|扁,biǎn|窆,biǎn|匾,biǎn|貶,biǎn|惼,biǎn|碥,biǎn|稨,biǎn|褊,biǎn|鴘,biǎn|藊,biǎn|釆,biǎn|辧,biǎn|疺,biǎn|覵,biǎn|鶣,biǎn|卞,biàn|弁,biàn|忭,biàn|抃,biàn|汳,biàn|汴,biàn|苄,biàn|峅,biàn|便,biàn|变,biàn|変,biàn|昪,biàn|覍,biàn|缏,biàn|遍,biàn|閞,biàn|辡,biàn|緶,biàn|艑,biàn|辨,biàn|辩,biàn|辫,biàn|辮,biàn|辯,biàn|變,biàn|彪,biāo|标,biāo|飑,biāo|骉,biāo|髟,biāo|淲,biāo|猋,biāo|脿,biāo|墂,biāo|幖,biāo|滮,biāo|蔈,biāo|骠,biāo|標,biāo|熛,biāo|膘,biāo|麃,biāo|瘭,biāo|镖,biāo|飙,biāo|飚,biāo|儦,biāo|颷,biāo|瀌,biāo|藨,biāo|謤,biāo|爂,biāo|臕,biāo|贆,biāo|鏢,biāo|穮,biāo|镳,biāo|飆,biāo|飇,biāo|飈,biāo|飊,biāo|驃,biāo|鑣,biāo|驫,biāo|摽,biāo|膔,biāo|篻,biāo|僄,biāo|徱,biāo|表,biǎo|婊,biǎo|裱,biǎo|褾,biǎo|錶,biǎo|檦,biǎo|諘,biǎo|俵,biào|鳔,biào|鰾,biào|憋,biē|鳖,biē|鱉,biē|鼈,biē|虌,biē|龞,biē|蟞,biē|別,bié|别,bié|莂,bié|蛂,bié|徶,bié|襒,bié|蹩,bié|穪,bié|瘪,biě|癟,biě|彆,biè|汃,bīn|邠,bīn|砏,bīn|宾,bīn|彬,bīn|斌,bīn|椕,bīn|滨,bīn|缤,bīn|槟,bīn|瑸,bīn|豩,bīn|賓,bīn|賔,bīn|镔,bīn|儐,bīn|濒,bīn|濱,bīn|濵,bīn|虨,bīn|豳,bīn|璸,bīn|瀕,bīn|霦,bīn|繽,bīn|蠙,bīn|鑌,bīn|顮,bīn|檳,bīn|玢,bīn|訜,bīn|傧,bīn|氞,bìn|摈,bìn|殡,bìn|膑,bìn|髩,bìn|擯,bìn|鬂,bìn|臏,bìn|髌,bìn|鬓,bìn|髕,bìn|鬢,bìn|殯,bìn|仌,bīng|氷,bīng|冰,bīng|兵,bīng|栟,bīng|掤,bīng|梹,bīng|鋲,bīng|幷,bīng|丙,bǐng|邴,bǐng|陃,bǐng|怲,bǐng|抦,bǐng|秉,bǐng|苪,bǐng|昞,bǐng|昺,bǐng|柄,bǐng|炳,bǐng|饼,bǐng|眪,bǐng|窉,bǐng|蛃,bǐng|禀,bǐng|鈵,bǐng|鉼,bǐng|鞆,bǐng|餅,bǐng|餠,bǐng|燷,bǐng|庰,bǐng|偋,bǐng|寎,bǐng|綆,bǐng|稟,bǐng|癛,bǐng|癝,bǐng|琕,bǐng|棅,bǐng|并,bìng|並,bìng|併,bìng|垪,bìng|倂,bìng|栤,bìng|病,bìng|竝,bìng|傡,bìng|摒,bìng|誁,bìng|靐,bìng|疒,bìng|啵,bo|蔔,bo|卜,bo|噃,bo|趵,bō|癶,bō|拨,bō|波,bō|玻,bō|袚,bō|袯,bō|钵,bō|饽,bō|紴,bō|缽,bō|菠,bō|碆,bō|鉢,bō|僠,bō|嶓,bō|撥,bō|播,bō|餑,bō|磻,bō|蹳,bō|驋,bō|鱍,bō|帗,bō|盋,bō|脖,bó|仢,bó|伯,bó|孛,bó|犻,bó|驳,bó|帛,bó|泊,bó|狛,bó|苩,bó|侼,bó|勃,bó|胉,bó|郣,bó|亳,bó|挬,bó|浡,bó|瓟,bó|秡,bó|钹,bó|铂,bó|桲,bó|淿,bó|舶,bó|博,bó|渤,bó|湐,bó|葧,bó|鹁,bó|愽,bó|搏,bó|猼,bó|鈸,bó|鉑,bó|馎,bó|僰,bó|煿,bó|箔,bó|膊,bó|艊,bó|馛,bó|駁,bó|踣,bó|鋍,bó|镈,bó|壆,bó|馞,bó|駮,bó|豰,bó|嚗,bó|懪,bó|礡,bó|簙,bó|鎛,bó|餺,bó|鵓,bó|犦,bó|髆,bó|髉,bó|欂,bó|襮,bó|礴,bó|鑮,bó|肑,bó|茀,bó|袹,bó|穛,bó|彴,bó|瓝,bó|牔,bó|蚾,bǒ|箥,bǒ|跛,bǒ|簸,bò|孹,bò|擘,bò|檗,bò|糪,bò|譒,bò|蘗,bò|襎,bò|檘,bò|蔢,bò|峬,bū|庯,bū|逋,bū|钸,bū|晡,bū|鈽,bū|誧,bū|餔,bū|鵏,bū|秿,bū|陠,bū|鯆,bū|轐,bú|醭,bú|不,bú|輹,bú|卟,bǔ|补,bǔ|哺,bǔ|捕,bǔ|補,bǔ|鳪,bǔ|獛,bǔ|鸔,bǔ|擈,bǔ|佈,bù|吥,bù|步,bù|咘,bù|怖,bù|歨,bù|歩,bù|钚,bù|勏,bù|埗,bù|悑,bù|捗,bù|荹,bù|部,bù|埠,bù|瓿,bù|鈈,bù|廍,bù|蔀,bù|踄,bù|郶,bù|篰,bù|餢,bù|簿,bù|尃,bù|箁,bù|抪,bù|柨,bù|布,bù|擦,cā|攃,cā|礤,cǎ|礸,cǎ|遪,cà|偲,cāi|猜,cāi|揌,cāi|才,cái|材,cái|财,cái|財,cái|戝,cái|裁,cái|采,cǎi|倸,cǎi|埰,cǎi|婇,cǎi|寀,cǎi|彩,cǎi|採,cǎi|睬,cǎi|跴,cǎi|綵,cǎi|踩,cǎi|菜,cài|棌,cài|蔡,cài|縩,cài|乲,cal|参,cān|參,cān|飡,cān|骖,cān|喰,cān|湌,cān|傪,cān|嬠,cān|餐,cān|驂,cān|嵾,cān|飱,cān|残,cán|蚕,cán|惭,cán|殘,cán|慚,cán|蝅,cán|慙,cán|蠶,cán|蠺,cán|惨,cǎn|慘,cǎn|噆,cǎn|憯,cǎn|黪,cǎn|黲,cǎn|灿,càn|粲,càn|儏,càn|澯,càn|薒,càn|燦,càn|璨,càn|爘,càn|謲,càn|仓,cāng|沧,cāng|苍,cāng|倉,cāng|舱,cāng|凔,cāng|嵢,cāng|滄,cāng|獊,cāng|蒼,cāng|濸,cāng|艙,cāng|螥,cāng|罉,cāng|藏,cáng|欌,cáng|鑶,cáng|賶,càng|撡,cāo|操,cāo|糙,cāo|曺,cáo|嘈,cáo|嶆,cáo|漕,cáo|蓸,cáo|槽,cáo|褿,cáo|艚,cáo|螬,cáo|鏪,cáo|慒,cáo|曹,cáo|艹,cǎo|艸,cǎo|草,cǎo|愺,cǎo|懆,cǎo|騲,cǎo|慅,cǎo|肏,cào|鄵,cào|襙,cào|冊,cè|册,cè|侧,cè|厕,cè|恻,cè|拺,cè|测,cè|荝,cè|敇,cè|側,cè|粣,cè|萗,cè|廁,cè|惻,cè|測,cè|策,cè|萴,cè|筞,cè|蓛,cè|墄,cè|箣,cè|憡,cè|刂,cè|厠,cè|膥,cēn|岑,cén|梣,cén|涔,cén|硶,cén|噌,cēng|层,céng|層,céng|竲,céng|驓,céng|曾,céng|蹭,cèng|硛,ceok|硳,ceok|岾,ceom|猠,ceon|乽,ceor|嚓,chā|叉,chā|扠,chā|芆,chā|杈,chā|肞,chā|臿,chā|訍,chā|偛,chā|嗏,chā|插,chā|銟,chā|锸,chā|艖,chā|疀,chā|鍤,chā|鎈,chā|垞,chá|查,chá|査,chá|茬,chá|茶,chá|嵖,chá|猹,chá|靫,chá|槎,chá|察,chá|碴,chá|褨,chá|檫,chá|搽,chá|衩,chǎ|镲,chǎ|鑔,chǎ|奼,chà|汊,chà|岔,chà|侘,chà|诧,chà|剎,chà|姹,chà|差,chà|紁,chà|詫,chà|拆,chāi|钗,chāi|釵,chāi|犲,chái|侪,chái|柴,chái|祡,chái|豺,chái|儕,chái|喍,chái|虿,chài|袃,chài|瘥,chài|蠆,chài|囆,chài|辿,chān|觇,chān|梴,chān|掺,chān|搀,chān|覘,chān|裧,chān|摻,chān|鋓,chān|幨,chān|襜,chān|攙,chān|嚵,chān|脠,chān|婵,chán|谗,chán|孱,chán|棎,chán|湹,chán|禅,chán|馋,chán|嬋,chán|煘,chán|缠,chán|獑,chán|蝉,chán|誗,chán|鋋,chán|儃,chán|廛,chán|潹,chán|潺,chán|緾,chán|磛,chán|禪,chán|毚,chán|鄽,chán|瀍,chán|蟬,chán|儳,chán|劖,chán|蟾,chán|酁,chán|壥,chán|巉,chán|瀺,chán|纏,chán|纒,chán|躔,chán|艬,chán|讒,chán|鑱,chán|饞,chán|繟,chán|澶,chán|镵,chán|产,chǎn|刬,chǎn|旵,chǎn|丳,chǎn|浐,chǎn|剗,chǎn|谄,chǎn|產,chǎn|産,chǎn|铲,chǎn|阐,chǎn|蒇,chǎn|剷,chǎn|嵼,chǎn|摌,chǎn|滻,chǎn|幝,chǎn|蕆,chǎn|諂,chǎn|閳,chǎn|燀,chǎn|簅,chǎn|冁,chǎn|醦,chǎn|闡,chǎn|囅,chǎn|灛,chǎn|讇,chǎn|墠,chǎn|骣,chǎn|鏟,chǎn|忏,chàn|硟,chàn|摲,chàn|懴,chàn|颤,chàn|懺,chàn|羼,chàn|韂,chàn|顫,chàn|伥,chāng|昌,chāng|倀,chāng|娼,chāng|淐,chāng|猖,chāng|菖,chāng|阊,chāng|晿,chāng|椙,chāng|琩,chāng|裮,chāng|锠,chāng|錩,chāng|閶,chāng|鲳,chāng|鯧,chāng|鼚,chāng|兏,cháng|肠,cháng|苌,cháng|尝,cháng|偿,cháng|常,cháng|徜,cháng|瓺,cháng|萇,cháng|甞,cháng|腸,cháng|嘗,cháng|嫦,cháng|瑺,cháng|膓,cháng|鋿,cháng|償,cháng|嚐,cháng|蟐,cháng|鲿,cháng|鏛,cháng|鱨,cháng|棖,cháng|尙,cháng|厂,chǎng|场,chǎng|昶,chǎng|場,chǎng|敞,chǎng|僘,chǎng|廠,chǎng|氅,chǎng|鋹,chǎng|惝,chǎng|怅,chàng|玚,chàng|畅,chàng|倡,chàng|鬯,chàng|唱,chàng|悵,chàng|暢,chàng|畼,chàng|誯,chàng|韔,chàng|抄,chāo|弨,chāo|怊,chāo|欩,chāo|钞,chāo|焯,chāo|超,chāo|鈔,chāo|繛,chāo|樔,chāo|绰,chāo|綽,chāo|綤,chāo|牊,cháo|巢,cháo|巣,cháo|朝,cháo|鄛,cháo|漅,cháo|嘲,cháo|潮,cháo|窲,cháo|罺,cháo|轈,cháo|晁,cháo|吵,chǎo|炒,chǎo|眧,chǎo|煼,chǎo|麨,chǎo|巐,chǎo|粆,chǎo|仦,chào|耖,chào|觘,chào|趠,chào|车,chē|車,chē|砗,chē|唓,chē|硨,chē|蛼,chē|莗,chē|扯,chě|偖,chě|撦,chě|彻,chè|坼,chè|迠,chè|烢,chè|聅,chè|掣,chè|硩,chè|頙,chè|徹,chè|撤,chè|澈,chè|勶,chè|瞮,chè|爡,chè|喢,chè|賝,chen|伧,chen|傖,chen|抻,chēn|郴,chēn|棽,chēn|琛,chēn|嗔,chēn|綝,chēn|諃,chēn|尘,chén|臣,chén|忱,chén|沉,chén|辰,chén|陈,chén|茞,chén|宸,chén|烥,chén|莐,chén|陳,chén|敐,chén|晨,chén|訦,chén|谌,chén|揨,chén|煁,chén|蔯,chén|塵,chén|樄,chén|瘎,chén|霃,chén|螴,chén|諶,chén|麎,chén|曟,chén|鷐,chén|薼,chén|趻,chěn|碜,chěn|墋,chěn|夦,chěn|磣,chěn|踸,chěn|贂,chěn|衬,chèn|疢,chèn|龀,chèn|趁,chèn|榇,chèn|齓,chèn|齔,chèn|嚫,chèn|谶,chèn|襯,chèn|讖,chèn|瀋,chèn|称,chēng|稱,chēng|阷,chēng|泟,chēng|柽,chēng|爯,chēng|棦,chēng|浾,chēng|偁,chēng|蛏,chēng|铛,chēng|牚,chēng|琤,chēng|赪,chēng|憆,chēng|摚,chēng|靗,chēng|撐,chēng|撑,chēng|緽,chēng|橕,chēng|瞠,chēng|赬,chēng|頳,chēng|檉,chēng|竀,chēng|蟶,chēng|鏳,chēng|鏿,chēng|饓,chēng|鐺,chēng|丞,chéng|成,chéng|呈,chéng|承,chéng|枨,chéng|诚,chéng|郕,chéng|乗,chéng|城,chéng|娍,chéng|宬,chéng|峸,chéng|洆,chéng|荿,chéng|乘,chéng|埕,chéng|挰,chéng|珹,chéng|掁,chéng|窚,chéng|脭,chéng|铖,chéng|堘,chéng|惩,chéng|椉,chéng|程,chéng|筬,chéng|絾,chéng|裎,chéng|塖,chéng|溗,chéng|碀,chéng|誠,chéng|畻,chéng|酲,chéng|鋮,chéng|澄,chéng|橙,chéng|檙,chéng|鯎,chéng|瀓,chéng|懲,chéng|騬,chéng|塍,chéng|悜,chěng|逞,chěng|骋,chěng|庱,chěng|睈,chěng|騁,chěng|秤,chèng|吃,chī|妛,chī|杘,chī|侙,chī|哧,chī|蚩,chī|鸱,chī|瓻,chī|眵,chī|笞,chī|訵,chī|嗤,chī|媸,chī|摛,chī|痴,chī|瞝,chī|螭,chī|鴟,chī|鵄,chī|癡,chī|魑,chī|齝,chī|攡,chī|麶,chī|彲,chī|黐,chī|蚳,chī|摴,chī|彨,chī|弛,chí|池,chí|驰,chí|迟,chí|岻,chí|茌,chí|持,chí|竾,chí|淔,chí|筂,chí|貾,chí|遅,chí|馳,chí|墀,chí|踟,chí|遲,chí|篪,chí|謘,chí|尺,chǐ|叺,chǐ|呎,chǐ|肔,chǐ|卶,chǐ|齿,chǐ|垑,chǐ|胣,chǐ|恥,chǐ|耻,chǐ|蚇,chǐ|豉,chǐ|欼,chǐ|歯,chǐ|裭,chǐ|鉹,chǐ|褫,chǐ|齒,chǐ|侈,chǐ|彳,chì|叱,chì|斥,chì|灻,chì|赤,chì|饬,chì|抶,chì|勅,chì|恜,chì|炽,chì|翄,chì|翅,chì|烾,chì|痓,chì|啻,chì|湁,chì|飭,chì|傺,chì|痸,chì|腟,chì|鉓,chì|雴,chì|憏,chì|翤,chì|遫,chì|慗,chì|瘛,chì|翨,chì|熾,chì|懘,chì|趩,chì|饎,chì|鶒,chì|鷘,chì|餝,chì|歗,chì|敕,chì|充,chōng|冲,chōng|忡,chōng|茺,chōng|珫,chōng|翀,chōng|舂,chōng|嘃,chōng|摏,chōng|憃,chōng|憧,chōng|衝,chōng|罿,chōng|艟,chōng|蹖,chōng|褈,chōng|傭,chōng|浺,chōng|虫,chóng|崇,chóng|崈,chóng|隀,chóng|蟲,chóng|宠,chǒng|埫,chǒng|寵,chǒng|沖,chòng|铳,chòng|銃,chòng|抽,chōu|紬,chōu|瘳,chōu|篘,chōu|犨,chōu|犫,chōu|跾,chōu|掫,chōu|仇,chóu|俦,chóu|栦,chóu|惆,chóu|绸,chóu|菗,chóu|畴,chóu|絒,chóu|愁,chóu|皗,chóu|稠,chóu|筹,chóu|酧,chóu|酬,chóu|綢,chóu|踌,chóu|儔,chóu|雔,chóu|嬦,chóu|懤,chóu|雠,chóu|疇,chóu|籌,chóu|躊,chóu|讎,chóu|讐,chóu|擣,chóu|燽,chóu|丑,chǒu|丒,chǒu|吜,chǒu|杽,chǒu|侴,chǒu|瞅,chǒu|醜,chǒu|矁,chǒu|魗,chǒu|臭,chòu|遚,chòu|殠,chòu|榋,chu|橻,chu|屮,chū|出,chū|岀,chū|初,chū|樗,chū|貙,chū|齣,chū|刍,chú|除,chú|厨,chú|滁,chú|蒢,chú|豠,chú|锄,chú|耡,chú|蒭,chú|蜍,chú|趎,chú|鉏,chú|雏,chú|犓,chú|廚,chú|篨,chú|鋤,chú|橱,chú|懨,chú|幮,chú|櫉,chú|蟵,chú|躇,chú|雛,chú|櫥,chú|蹰,chú|鶵,chú|躕,chú|媰,chú|杵,chǔ|础,chǔ|储,chǔ|楮,chǔ|禇,chǔ|楚,chǔ|褚,chǔ|濋,chǔ|儲,chǔ|檚,chǔ|璴,chǔ|礎,chǔ|齭,chǔ|齼,chǔ|処,chǔ|椘,chǔ|亍,chù|处,chù|竌,chù|怵,chù|拀,chù|绌,chù|豖,chù|竐,chù|俶,chù|敊,chù|珿,chù|絀,chù|處,chù|傗,chù|琡,chù|搐,chù|触,chù|踀,chù|閦,chù|儊,chù|憷,chù|斶,chù|歜,chù|臅,chù|黜,chù|觸,chù|矗,chù|觕,chù|畜,chù|鄐,chù|搋,chuāi|揣,chuāi|膗,chuái|嘬,chuài|踹,chuài|膪,chuài|巛,chuān|川,chuān|氚,chuān|穿,chuān|剶,chuān|瑏,chuān|传,chuán|舡,chuán|船,chuán|猭,chuán|遄,chuán|傳,chuán|椽,chuán|歂,chuán|暷,chuán|輲,chuán|甎,chuán|舛,chuǎn|荈,chuǎn|喘,chuǎn|僢,chuǎn|堾,chuǎn|踳,chuǎn|汌,chuàn|串,chuàn|玔,chuàn|钏,chuàn|釧,chuàn|賗,chuàn|刅,chuāng|炊,chuī|龡,chuī|圌,chuí|垂,chuí|桘,chuí|陲,chuí|捶,chuí|菙,chuí|棰,chuí|槌,chuí|锤,chuí|箠,chuí|顀,chuí|錘,chuí|鰆,chun|旾,chūn|杶,chūn|春,chūn|萅,chūn|媋,chūn|暙,chūn|椿,chūn|槆,chūn|瑃,chūn|箺,chūn|蝽,chūn|橁,chūn|輴,chūn|櫄,chūn|鶞,chūn|纯,chún|陙,chún|唇,chún|浱,chún|純,chún|莼,chún|淳,chún|脣,chún|犉,chún|滣,chún|鹑,chún|漘,chún|醇,chún|醕,chún|鯙,chún|鶉,chún|蒓,chún|偆,chǔn|萶,chǔn|惷,chǔn|睶,chǔn|賰,chǔn|蠢,chǔn|踔,chuō|戳,chuō|啜,chuò|辵,chuò|娕,chuò|娖,chuò|惙,chuò|涰,chuò|逴,chuò|辍,chuò|酫,chuò|龊,chuò|擉,chuò|磭,chuò|歠,chuò|嚽,chuò|齪,chuò|鑡,chuò|齱,chuò|婼,chuò|鋜,chuò|輟,chuò|呲,cī|玼,cī|疵,cī|趀,cī|偨,cī|縒,cī|跐,cī|髊,cī|齹,cī|枱,cī|词,cí|珁,cí|垐,cí|柌,cí|祠,cí|茨,cí|瓷,cí|詞,cí|辝,cí|慈,cí|甆,cí|辞,cí|鈶,cí|雌,cí|鹚,cí|糍,cí|辤,cí|飺,cí|餈,cí|嬨,cí|濨,cí|鴜,cí|礠,cí|辭,cí|鶿,cí|鷀,cí|磁,cí|此,cǐ|佌,cǐ|皉,cǐ|朿,cì|次,cì|佽,cì|刺,cì|刾,cì|庛,cì|茦,cì|栨,cì|莿,cì|絘,cì|赐,cì|螆,cì|賜,cì|蛓,cì|嗭,cis|囱,cōng|匆,cōng|囪,cōng|苁,cōng|忩,cōng|枞,cōng|茐,cōng|怱,cōng|悤,cōng|棇,cōng|焧,cōng|葱,cōng|楤,cōng|漗,cōng|聡,cōng|蔥,cōng|骢,cōng|暰,cōng|樅,cōng|樬,cōng|瑽,cōng|璁,cōng|聪,cōng|瞛,cōng|篵,cōng|聰,cōng|蟌,cōng|繱,cōng|鏦,cōng|騘,cōng|驄,cōng|聦,cōng|从,cóng|從,cóng|丛,cóng|従,cóng|婃,cóng|孮,cóng|徖,cóng|悰,cóng|淙,cóng|琮,cóng|漎,cóng|誴,cóng|賨,cóng|賩,cóng|樷,cóng|藂,cóng|叢,cóng|灇,cóng|欉,cóng|爜,cóng|憁,còng|謥,còng|凑,còu|湊,còu|楱,còu|腠,còu|辏,còu|輳,còu|粗,cū|麁,cū|麄,cū|麤,cū|徂,cú|殂,cú|蔖,cǔ|促,cù|猝,cù|媨,cù|瘄,cù|蔟,cù|誎,cù|趗,cù|憱,cù|醋,cù|瘯,cù|簇,cù|縬,cù|鼀,cù|蹴,cù|蹵,cù|顣,cù|蹙,cù|汆,cuān|撺,cuān|镩,cuān|蹿,cuān|攛,cuān|躥,cuān|鑹,cuān|攅,cuán|櫕,cuán|巑,cuán|攢,cuán|窜,cuàn|熶,cuàn|篡,cuàn|殩,cuàn|篹,cuàn|簒,cuàn|竄,cuàn|爨,cuàn|乼,cui|崔,cuī|催,cuī|凗,cuī|墔,cuī|摧,cuī|榱,cuī|獕,cuī|磪,cuī|鏙,cuī|漼,cuī|慛,cuī|璀,cuǐ|皠,cuǐ|熣,cuǐ|繀,cuǐ|忰,cuì|疩,cuì|翆,cuì|脃,cuì|脆,cuì|啐,cuì|啛,cuì|悴,cuì|淬,cuì|萃,cuì|毳,cuì|焠,cuì|瘁,cuì|粹,cuì|膵,cuì|膬,cuì|竁,cuì|臎,cuì|琗,cuì|粋,cuì|脺,cuì|翠,cuì|邨,cūn|村,cūn|皴,cūn|澊,cūn|竴,cūn|存,cún|刌,cǔn|忖,cǔn|寸,cùn|籿,cùn|襊,cuō|搓,cuō|瑳,cuō|遳,cuō|磋,cuō|撮,cuō|蹉,cuō|醝,cuō|虘,cuó|嵯,cuó|痤,cuó|矬,cuó|蒫,cuó|鹾,cuó|鹺,cuó|嵳,cuó|脞,cuǒ|剉,cuò|剒,cuò|厝,cuò|夎,cuò|挫,cuò|莝,cuò|莡,cuò|措,cuò|逪,cuò|棤,cuò|锉,cuò|蓌,cuò|错,cuò|銼,cuò|錯,cuò|疸,da|咑,dā|哒,dā|耷,dā|畣,dā|搭,dā|嗒,dā|噠,dā|撘,dā|鎝,dā|笚,dā|矺,dā|褡,dā|墶,dá|达,dá|迏,dá|迖,dá|妲,dá|怛,dá|垯,dá|炟,dá|羍,dá|荅,dá|荙,dá|剳,dá|匒,dá|笪,dá|逹,dá|溚,dá|答,dá|詚,dá|達,dá|跶,dá|瘩,dá|靼,dá|薘,dá|鞑,dá|燵,dá|蟽,dá|鎉,dá|躂,dá|鐽,dá|韃,dá|龖,dá|龘,dá|搨,dá|繨,dá|打,dǎ|觰,dǎ|大,dà|亣,dà|眔,dà|橽,dà|汏,dà|呆,dāi|獃,dāi|懛,dāi|歹,dǎi|傣,dǎi|逮,dǎi|代,dài|轪,dài|侢,dài|垈,dài|岱,dài|帒,dài|甙,dài|绐,dài|迨,dài|带,dài|待,dài|柋,dài|殆,dài|玳,dài|贷,dài|帯,dài|軑,dài|埭,dài|帶,dài|紿,dài|蚮,dài|袋,dài|軚,dài|貸,dài|軩,dài|瑇,dài|廗,dài|叇,dài|曃,dài|緿,dài|鮘,dài|鴏,dài|戴,dài|艜,dài|黛,dài|簤,dài|蹛,dài|瀻,dài|霴,dài|襶,dài|靆,dài|螮,dài|蝳,dài|跢,dài|箉,dài|骀,dài|怠,dài|黱,dài|愖,dān|丹,dān|妉,dān|单,dān|担,dān|単,dān|眈,dān|砃,dān|耼,dān|耽,dān|郸,dān|聃,dān|躭,dān|酖,dān|單,dān|媅,dān|殚,dān|瘅,dān|匰,dān|箪,dān|褝,dān|鄲,dān|頕,dān|儋,dān|勯,dān|擔,dān|殫,dān|癉,dān|襌,dān|簞,dān|瓭,dān|卩,dān|亻,dān|娊,dān|噡,dān|聸,dān|伔,dǎn|刐,dǎn|狚,dǎn|玬,dǎn|胆,dǎn|衴,dǎn|紞,dǎn|掸,dǎn|亶,dǎn|馾,dǎn|撣,dǎn|澸,dǎn|黕,dǎn|膽,dǎn|丼,dǎn|抌,dǎn|赕,dǎn|賧,dǎn|黵,dǎn|黮,dǎn|繵,dàn|譂,dàn|旦,dàn|但,dàn|帎,dàn|沊,dàn|泹,dàn|诞,dàn|柦,dàn|疍,dàn|啖,dàn|啗,dàn|弹,dàn|惮,dàn|淡,dàn|蛋,dàn|啿,dàn|氮,dàn|腅,dàn|蜑,dàn|觛,dàn|窞,dàn|誕,dàn|僤,dàn|噉,dàn|髧,dàn|嘾,dàn|彈,dàn|憚,dàn|憺,dàn|澹,dàn|禫,dàn|餤,dàn|駳,dàn|鴠,dàn|甔,dàn|癚,dàn|嚪,dàn|贉,dàn|霮,dàn|饏,dàn|蟺,dàn|倓,dàn|惔,dàn|弾,dàn|醈,dàn|撢,dàn|萏,dàn|当,dāng|珰,dāng|裆,dāng|筜,dāng|儅,dāng|噹,dāng|澢,dāng|璫,dāng|襠,dāng|簹,dāng|艡,dāng|蟷,dāng|當,dāng|挡,dǎng|党,dǎng|谠,dǎng|擋,dǎng|譡,dǎng|黨,dǎng|灙,dǎng|欓,dǎng|讜,dǎng|氹,dàng|凼,dàng|圵,dàng|宕,dàng|砀,dàng|垱,dàng|荡,dàng|档,dàng|菪,dàng|瓽,dàng|逿,dàng|潒,dàng|碭,dàng|瞊,dàng|蕩,dàng|趤,dàng|壋,dàng|檔,dàng|璗,dàng|盪,dàng|礑,dàng|簜,dàng|蘯,dàng|闣,dàng|愓,dàng|嵣,dàng|偒,dàng|雼,dàng|裯,dāo|刀,dāo|叨,dāo|屶,dāo|忉,dāo|氘,dāo|舠,dāo|釖,dāo|鱽,dāo|魛,dāo|虭,dāo|捯,dáo|导,dǎo|岛,dǎo|陦,dǎo|倒,dǎo|宲,dǎo|捣,dǎo|祷,dǎo|禂,dǎo|搗,dǎo|隝,dǎo|嶋,dǎo|嶌,dǎo|槝,dǎo|導,dǎo|隯,dǎo|壔,dǎo|嶹,dǎo|蹈,dǎo|禱,dǎo|菿,dǎo|島,dǎo|帱,dào|幬,dào|到,dào|悼,dào|盗,dào|椡,dào|盜,dào|道,dào|稲,dào|翢,dào|噵,dào|稻,dào|衜,dào|檤,dào|衟,dào|翿,dào|軇,dào|瓙,dào|纛,dào|箌,dào|的,de|嘚,dē|恴,dé|得,dé|淂,dé|悳,dé|惪,dé|锝,dé|徳,dé|德,dé|鍀,dé|棏,dé|揼,dem|扥,den|扽,den|灯,dēng|登,dēng|豋,dēng|噔,dēng|嬁,dēng|燈,dēng|璒,dēng|竳,dēng|簦,dēng|艠,dēng|覴,dēng|蹬,dēng|墱,dēng|戥,děng|等,děng|澂,dèng|邓,dèng|僜,dèng|凳,dèng|鄧,dèng|隥,dèng|嶝,dèng|瞪,dèng|磴,dèng|镫,dèng|櫈,dèng|鐙,dèng|仾,dī|低,dī|奃,dī|彽,dī|袛,dī|啲,dī|埞,dī|羝,dī|隄,dī|堤,dī|趆,dī|嘀,dī|滴,dī|磾,dī|鍉,dī|鞮,dī|氐,dī|牴,dī|碮,dī|踧,dí|镝,dí|廸,dí|狄,dí|籴,dí|苖,dí|迪,dí|唙,dí|敌,dí|涤,dí|荻,dí|梑,dí|笛,dí|觌,dí|靮,dí|滌,dí|髢,dí|嫡,dí|蔋,dí|蔐,dí|頔,dí|魡,dí|敵,dí|篴,dí|嚁,dí|藡,dí|豴,dí|糴,dí|覿,dí|鸐,dí|藋,dí|鬄,dí|樀,dí|蹢,dí|鏑,dí|泜,dǐ|诋,dǐ|邸,dǐ|阺,dǐ|呧,dǐ|坻,dǐ|底,dǐ|弤,dǐ|抵,dǐ|拞,dǐ|柢,dǐ|砥,dǐ|掋,dǐ|菧,dǐ|詆,dǐ|軧,dǐ|聜,dǐ|骶,dǐ|鯳,dǐ|坘,dǐ|厎,dǐ|赿,dì|地,dì|弚,dì|坔,dì|弟,dì|旳,dì|杕,dì|玓,dì|怟,dì|枤,dì|苐,dì|帝,dì|埊,dì|娣,dì|递,dì|逓,dì|偙,dì|啇,dì|梊,dì|焍,dì|眱,dì|祶,dì|第,dì|菂,dì|谛,dì|釱,dì|媂,dì|棣,dì|睇,dì|缔,dì|蒂,dì|僀,dì|禘,dì|腣,dì|遞,dì|鉪,dì|馰,dì|墑,dì|墬,dì|摕,dì|碲,dì|蝃,dì|遰,dì|慸,dì|甋,dì|締,dì|嶳,dì|諦,dì|踶,dì|弔,dì|嵽,dì|諟,dì|珶,dì|渧,dì|蹏,dì|揥,dì|墆,dì|疐,dì|俤,dì|蔕,dì|嗲,diǎ|敁,diān|掂,diān|傎,diān|厧,diān|嵮,diān|滇,diān|槙,diān|瘨,diān|颠,diān|蹎,diān|巅,diān|顚,diān|顛,diān|癫,diān|巓,diān|巔,diān|攧,diān|癲,diān|齻,diān|槇,diān|典,diǎn|点,diǎn|婰,diǎn|敟,diǎn|椣,diǎn|碘,diǎn|蒧,diǎn|蕇,diǎn|踮,diǎn|點,diǎn|痶,diǎn|丶,diǎn|奌,diǎn|电,diàn|佃,diàn|甸,diàn|坫,diàn|店,diàn|垫,diàn|扂,diàn|玷,diàn|钿,diàn|唸,diàn|婝,diàn|惦,diàn|淀,diàn|奠,diàn|琔,diàn|殿,diàn|蜔,diàn|鈿,diàn|電,diàn|墊,diàn|橂,diàn|澱,diàn|靛,diàn|磹,diàn|癜,diàn|簟,diàn|驔,diàn|腍,diàn|橝,diàn|壂,diàn|刁,diāo|叼,diāo|汈,diāo|刟,diāo|凋,diāo|奝,diāo|弴,diāo|彫,diāo|蛁,diāo|琱,diāo|貂,diāo|碉,diāo|鳭,diāo|殦,diāo|雕,diāo|鮉,diāo|鲷,diāo|簓,diāo|鼦,diāo|鯛,diāo|鵰,diāo|颩,diāo|矵,diāo|錭,diāo|淍,diāo|屌,diǎo|鸼,diǎo|鵃,diǎo|扚,diǎo|伄,diào|吊,diào|钓,diào|窎,diào|訋,diào|调,diào|掉,diào|釣,diào|铞,diào|鈟,diào|竨,diào|銱,diào|雿,diào|調,diào|瘹,diào|窵,diào|鋽,diào|鑃,diào|誂,diào|嬥,diào|絩,diào|爹,diē|跌,diē|褺,diē|跮,dié|苵,dié|迭,dié|垤,dié|峌,dié|恎,dié|绖,dié|胅,dié|瓞,dié|眣,dié|耊,dié|啑,dié|戜,dié|谍,dié|喋,dié|堞,dié|幉,dié|惵,dié|揲,dié|畳,dié|絰,dié|耋,dié|臷,dié|詄,dié|趃,dié|叠,dié|殜,dié|牃,dié|牒,dié|镻,dié|碟,dié|蜨,dié|褋,dié|艓,dié|蝶,dié|諜,dié|蹀,dié|鲽,dié|曡,dié|鰈,dié|疉,dié|疊,dié|氎,dié|渉,dié|崼,dié|鮙,dié|跕,dié|鐡,dié|怢,dié|槢,dié|挃,dié|柣,dié|螲,dié|疂,dié|眰,diè|嚸,dim|丁,dīng|仃,dīng|叮,dīng|帄,dīng|玎,dīng|甼,dīng|疔,dīng|盯,dīng|耵,dīng|靪,dīng|奵,dīng|町,dīng|虰,dīng|酊,dǐng|顶,dǐng|頂,dǐng|鼎,dǐng|鼑,dǐng|薡,dǐng|鐤,dǐng|顁,dǐng|艼,dǐng|濎,dǐng|嵿,dǐng|钉,dìng|釘,dìng|订,dìng|忊,dìng|饤,dìng|矴,dìng|定,dìng|訂,dìng|飣,dìng|啶,dìng|萣,dìng|椗,dìng|腚,dìng|碇,dìng|锭,dìng|碠,dìng|聢,dìng|錠,dìng|磸,dìng|铤,dìng|鋌,dìng|掟,dìng|丟,diū|丢,diū|铥,diū|銩,diū|东,dōng|冬,dōng|咚,dōng|東,dōng|苳,dōng|昸,dōng|氡,dōng|倲,dōng|鸫,dōng|埬,dōng|娻,dōng|崬,dōng|涷,dōng|笗,dōng|菄,dōng|氭,dōng|蝀,dōng|鮗,dōng|鼕,dōng|鯟,dōng|鶇,dōng|鶫,dōng|徚,dōng|夂,dōng|岽,dōng|揰,dǒng|董,dǒng|墥,dǒng|嬞,dǒng|懂,dǒng|箽,dǒng|蕫,dǒng|諌,dǒng|湩,dǒng|动,dòng|冻,dòng|侗,dòng|垌,dòng|峒,dòng|峝,dòng|恫,dòng|挏,dòng|栋,dòng|洞,dòng|胨,dòng|迵,dòng|凍,dòng|戙,dòng|胴,dòng|動,dòng|崠,dòng|硐,dòng|棟,dòng|腖,dòng|働,dòng|詷,dòng|駧,dòng|霘,dòng|狫,dòng|烔,dòng|絧,dòng|衕,dòng|勭,dòng|騆,dòng|姛,dòng|瞗,dōu|吺,dōu|剅,dōu|唗,dōu|都,dōu|兜,dōu|兠,dōu|蔸,dōu|橷,dōu|篼,dōu|侸,dōu|艔,dóu|乧,dǒu|阧,dǒu|抖,dǒu|枓,dǒu|陡,dǒu|蚪,dǒu|鈄,dǒu|斗,dòu|豆,dòu|郖,dòu|浢,dòu|荳,dòu|逗,dòu|饾,dòu|鬥,dòu|梪,dòu|毭,dòu|脰,dòu|酘,dòu|痘,dòu|閗,dòu|窦,dòu|鬦,dòu|鋀,dòu|餖,dòu|斣,dòu|闘,dòu|竇,dòu|鬪,dòu|鬭,dòu|凟,dòu|鬬,dòu|剢,dū|阇,dū|嘟,dū|督,dū|醏,dū|闍,dū|厾,dū|毒,dú|涜,dú|读,dú|渎,dú|椟,dú|牍,dú|犊,dú|裻,dú|読,dú|獨,dú|錖,dú|匵,dú|嬻,dú|瀆,dú|櫝,dú|殰,dú|牘,dú|犢,dú|瓄,dú|皾,dú|騳,dú|讀,dú|豄,dú|贕,dú|韣,dú|髑,dú|鑟,dú|韇,dú|韥,dú|黷,dú|讟,dú|独,dú|樚,dú|襡,dú|襩,dú|黩,dú|笃,dǔ|堵,dǔ|帾,dǔ|琽,dǔ|赌,dǔ|睹,dǔ|覩,dǔ|賭,dǔ|篤,dǔ|暏,dǔ|笁,dǔ|陼,dǔ|芏,dù|妒,dù|杜,dù|肚,dù|妬,dù|度,dù|荰,dù|秺,dù|渡,dù|镀,dù|螙,dù|殬,dù|鍍,dù|蠧,dù|蠹,dù|剫,dù|晵,dù|靯,dù|篅,duān|偳,duān|媏,duān|端,duān|褍,duān|鍴,duān|剬,duān|短,duǎn|段,duàn|断,duàn|塅,duàn|缎,duàn|葮,duàn|椴,duàn|煅,duàn|瑖,duàn|腶,duàn|碫,duàn|锻,duàn|緞,duàn|毈,duàn|簖,duàn|鍛,duàn|斷,duàn|躖,duàn|煆,duàn|籪,duàn|叾,dug|搥,duī|鎚,duī|垖,duī|堆,duī|塠,duī|嵟,duī|痽,duī|磓,duī|頧,duī|鴭,duī|鐜,duī|埻,duī|謉,duǐ|錞,duì|队,duì|对,duì|兊,duì|兌,duì|兑,duì|対,duì|祋,duì|怼,duì|陮,duì|隊,duì|碓,duì|綐,duì|對,duì|憝,duì|濧,duì|薱,duì|镦,duì|懟,duì|瀩,duì|譈,duì|譵,duì|憞,duì|鋭,duì|杸,duì|吨,dūn|惇,dūn|敦,dūn|蜳,dūn|墩,dūn|墪,dūn|壿,dūn|撴,dūn|獤,dūn|噸,dūn|撉,dūn|橔,dūn|犜,dūn|礅,dūn|蹲,dūn|蹾,dūn|驐,dūn|鐓,dūn|盹,dǔn|趸,dǔn|躉,dǔn|伅,dùn|囤,dùn|庉,dùn|沌,dùn|炖,dùn|盾,dùn|砘,dùn|逇,dùn|钝,dùn|遁,dùn|鈍,dùn|腞,dùn|頓,dùn|碷,dùn|遯,dùn|潡,dùn|燉,dùn|踲,dùn|楯,dùn|腯,dùn|顿,dùn|多,duō|夛,duō|咄,duō|哆,duō|茤,duō|剟,duō|崜,duō|敠,duō|毲,duō|裰,duō|嚉,duō|掇,duō|仛,duó|夺,duó|铎,duó|敓,duó|敚,duó|喥,duó|敪,duó|鈬,duó|奪,duó|凙,duó|踱,duó|鮵,duó|鐸,duó|跿,duó|沰,duó|痥,duó|奲,duǒ|朵,duǒ|朶,duǒ|哚,duǒ|垛,duǒ|挅,duǒ|挆,duǒ|埵,duǒ|缍,duǒ|椯,duǒ|趓,duǒ|躱,duǒ|躲,duǒ|綞,duǒ|亸,duǒ|鬌,duǒ|嚲,duǒ|垜,duǒ|橢,duǒ|硾,duǒ|吋,duò|刴,duò|剁,duò|沲,duò|陊,duò|陏,duò|饳,duò|柮,duò|桗,duò|堕,duò|舵,duò|惰,duò|跥,duò|跺,duò|飿,duò|墮,duò|嶞,duò|憜,duò|墯,duò|鵽,duò|隓,duò|貀,duò|詑,duò|駄,duò|媠,duò|嫷,duò|尮,duò|呃,e|妸,ē|妿,ē|娿,ē|婀,ē|匼,ē|讹,é|吪,é|囮,é|迗,é|俄,é|娥,é|峨,é|峩,é|涐,é|莪,é|珴,é|訛,é|睋,é|鈋,é|锇,é|鹅,é|蛾,é|磀,é|誐,é|鋨,é|頟,é|额,é|魤,é|額,é|鵝,é|鵞,é|譌,é|騀,é|佮,é|鰪,é|皒,é|欸,ě|枙,ě|砈,ě|鵈,ě|玀,ě|閜,ě|砵,è|惡,è|厄,è|歺,è|屵,è|戹,è|岋,è|阨,è|扼,è|阸,è|呝,è|砐,è|轭,è|咢,è|咹,è|垩,è|姶,è|峉,è|匎,è|恶,è|砨,è|蚅,è|饿,è|偔,è|卾,è|堊,è|悪,è|硆,è|谔,è|軛,è|鄂,è|阏,è|堮,è|崿,è|愕,è|湂,è|萼,è|豟,è|軶,è|遏,è|廅,è|搤,è|搹,è|琧,è|腭,è|詻,è|僫,è|蝁,è|锷,è|鹗,è|蕚,è|遻,è|頞,è|颚,è|餓,è|噩,è|擜,è|覨,è|諤,è|餩,è|鍔,è|鳄,è|歞,è|顎,è|櫮,è|鰐,è|鶚,è|讍,è|齶,è|鱷,è|齃,è|啈,è|搕,è|礘,è|魥,è|蘁,è|齾,è|苊,è|遌,è|鑩,è|诶,ēi|誒,ēi|奀,ēn|恩,ēn|蒽,ēn|煾,ēn|唔,én|峎,ěn|摁,èn|嗯,èn|鞥,eng|仒,eo|乻,eol|旕,eos|儿,ér|而,ér|児,ér|侕,ér|兒,ér|陑,ér|峏,ér|洏,ér|耏,ér|荋,ér|栭,ér|胹,ér|唲,ér|袻,ér|鸸,ér|粫,ér|聏,ér|輀,ér|隭,ér|髵,ér|鮞,ér|鴯,ér|轜,ér|咡,ér|杒,ér|陾,ér|輭,ér|鲕,ér|尒,ěr|尓,ěr|尔,ěr|耳,ěr|迩,ěr|洱,ěr|饵,ěr|栮,ěr|毦,ěr|珥,ěr|铒,ěr|爾,ěr|鉺,ěr|餌,ěr|駬,ěr|薾,ěr|邇,ěr|趰,ěr|嬭,ěr|二,èr|弍,èr|弐,èr|佴,èr|刵,èr|贰,èr|衈,èr|貳,èr|誀,èr|樲,èr|髶,èr|貮,èr|发,fā|沷,fā|発,fā|發,fā|彂,fā|髪,fā|橃,fā|醗,fā|乏,fá|伐,fá|姂,fá|垡,fá|罚,fá|阀,fá|栰,fá|傠,fá|筏,fá|瞂,fá|罰,fá|閥,fá|罸,fá|藅,fá|汎,fá|佱,fǎ|法,fǎ|鍅,fǎ|灋,fǎ|砝,fǎ|珐,fà|琺,fà|髮,fà|蕟,fà|帆,fān|忛,fān|犿,fān|番,fān|勫,fān|墦,fān|嬏,fān|幡,fān|憣,fān|旙,fān|旛,fān|翻,fān|藩,fān|轓,fān|颿,fān|飜,fān|鱕,fān|蕃,fān|凡,fán|凢,fán|凣,fán|匥,fán|杋,fán|柉,fán|籵,fán|钒,fán|舤,fán|烦,fán|舧,fán|笲,fán|釩,fán|棥,fán|煩,fán|緐,fán|樊,fán|橎,fán|燔,fán|璠,fán|薠,fán|繁,fán|繙,fán|羳,fán|蹯,fán|瀿,fán|礬,fán|蘩,fán|鐇,fán|蠜,fán|鷭,fán|氾,fán|瀪,fán|渢,fán|伋,fán|舩,fán|矾,fán|反,fǎn|仮,fǎn|辺,fǎn|返,fǎn|攵,fǎn|犭,fǎn|払,fǎn|犯,fàn|奿,fàn|泛,fàn|饭,fàn|范,fàn|贩,fàn|畈,fàn|訉,fàn|軓,fàn|梵,fàn|盕,fàn|笵,fàn|販,fàn|軬,fàn|飯,fàn|飰,fàn|滼,fàn|嬎,fàn|範,fàn|嬔,fàn|婏,fàn|方,fāng|邡,fāng|坊,fāng|芳,fāng|牥,fāng|钫,fāng|淓,fāng|堏,fāng|鈁,fāng|錺,fāng|鴋,fāng|埅,fāng|枋,fāng|防,fáng|妨,fáng|房,fáng|肪,fáng|鲂,fáng|魴,fáng|仿,fǎng|访,fǎng|纺,fǎng|昉,fǎng|昘,fǎng|瓬,fǎng|眆,fǎng|倣,fǎng|旊,fǎng|紡,fǎng|舫,fǎng|訪,fǎng|髣,fǎng|鶭,fǎng|放,fàng|飞,fēi|妃,fēi|非,fēi|飛,fēi|啡,fēi|婓,fēi|婔,fēi|渄,fēi|绯,fēi|菲,fēi|扉,fēi|猆,fēi|靟,fēi|裶,fēi|緋,fēi|蜚,fēi|霏,fēi|鲱,fēi|餥,fēi|馡,fēi|騑,fēi|騛,fēi|鯡,fēi|飝,fēi|奜,fēi|肥,féi|淝,féi|暃,féi|腓,féi|蜰,féi|棐,féi|萉,féi|蟦,féi|朏,fěi|胐,fěi|匪,fěi|诽,fěi|悱,fěi|斐,fěi|榧,fěi|翡,fěi|蕜,fěi|誹,fěi|篚,fěi|襏,fèi|吠,fèi|废,fèi|沸,fèi|狒,fèi|肺,fèi|昲,fèi|费,fèi|俷,fèi|剕,fèi|厞,fèi|疿,fèi|屝,fèi|廃,fèi|費,fèi|痱,fèi|廢,fèi|曊,fèi|癈,fèi|鼣,fèi|濷,fèi|櫠,fèi|鐨,fèi|靅,fèi|蕡,fèi|芾,fèi|笰,fèi|紼,fèi|髴,fèi|柹,fèi|胏,fèi|镄,fèi|吩,fēn|帉,fēn|纷,fēn|芬,fēn|昐,fēn|氛,fēn|竕,fēn|紛,fēn|翂,fēn|棻,fēn|躮,fēn|酚,fēn|鈖,fēn|雰,fēn|朆,fēn|餴,fēn|饙,fēn|錀,fēn|坟,fén|妢,fén|岎,fén|汾,fén|枌,fén|梤,fén|羒,fén|蚠,fén|蚡,fén|棼,fén|焚,fén|蒶,fén|馚,fén|隫,fén|墳,fén|幩,fén|魵,fén|橨,fén|燓,fén|豮,fén|鼢,fén|羵,fén|鼖,fén|豶,fén|轒,fén|馩,fén|黂,fén|鐼,fén|粉,fěn|瞓,fěn|黺,fěn|分,fèn|份,fèn|坋,fèn|弅,fèn|奋,fèn|忿,fèn|秎,fèn|偾,fèn|愤,fèn|粪,fèn|僨,fèn|憤,fèn|奮,fèn|膹,fèn|糞,fèn|鲼,fèn|瀵,fèn|鱝,fèn|丰,fēng|风,fēng|仹,fēng|凨,fēng|凬,fēng|妦,fēng|沣,fēng|沨,fēng|枫,fēng|封,fēng|疯,fēng|盽,fēng|砜,fēng|風,fēng|峯,fēng|峰,fēng|偑,fēng|桻,fēng|烽,fēng|琒,fēng|崶,fēng|溄,fēng|猦,fēng|葑,fēng|锋,fēng|楓,fēng|犎,fēng|蜂,fēng|瘋,fēng|碸,fēng|僼,fēng|篈,fēng|鄷,fēng|鋒,fēng|檒,fēng|豐,fēng|鎽,fēng|酆,fēng|寷,fēng|灃,fēng|蘴,fēng|靊,fēng|飌,fēng|麷,fēng|豊,fēng|凮,fēng|鏠,fēng|冯,féng|捀,féng|浲,féng|逢,féng|堸,féng|馮,féng|綘,féng|缝,féng|艂,féng|縫,féng|讽,fěng|唪,fěng|諷,fěng|凤,fèng|奉,fèng|甮,fèng|俸,fèng|湗,fèng|焨,fèng|煈,fèng|鳯,fèng|鳳,fèng|鴌,fèng|賵,fèng|蘕,fèng|赗,fèng|覅,fiao|仏,fó|佛,fó|坲,fó|梻,fó|垺,fóu|紑,fóu|缶,fǒu|否,fǒu|缹,fǒu|缻,fǒu|雬,fǒu|鴀,fǒu|芣,fǒu|夫,fū|邞,fū|呋,fū|姇,fū|枎,fū|玞,fū|肤,fū|怤,fū|砆,fū|胕,fū|荂,fū|衭,fū|娐,fū|荴,fū|旉,fū|紨,fū|趺,fū|酜,fū|麸,fū|稃,fū|跗,fū|鈇,fū|筟,fū|綒,fū|鄜,fū|孵,fū|豧,fū|敷,fū|膚,fū|鳺,fū|麩,fū|糐,fū|麬,fū|麱,fū|懯,fū|烰,fū|琈,fū|粰,fū|璷,fū|伕,fú|乀,fú|伏,fú|凫,fú|甶,fú|冹,fú|刜,fú|孚,fú|扶,fú|芙,fú|咈,fú|岪,fú|彿,fú|怫,fú|拂,fú|服,fú|泭,fú|绂,fú|绋,fú|苻,fú|俘,fú|垘,fú|柫,fú|氟,fú|洑,fú|炥,fú|玸,fú|祓,fú|罘,fú|茯,fú|郛,fú|韨,fú|鳬,fú|哹,fú|栿,fú|浮,fú|畗,fú|砩,fú|蚨,fú|匐,fú|桴,fú|涪,fú|符,fú|紱,fú|翇,fú|艴,fú|菔,fú|虙,fú|袱,fú|幅,fú|棴,fú|罦,fú|葍,fú|福,fú|綍,fú|艀,fú|蜉,fú|辐,fú|鉘,fú|鉜,fú|颫,fú|鳧,fú|榑,fú|稪,fú|箙,fú|複,fú|韍,fú|幞,fú|澓,fú|蝠,fú|鴔,fú|諨,fú|輻,fú|鮄,fú|癁,fú|鮲,fú|黻,fú|鵩,fú|坿,fú|汱,fú|酻,fú|弗,fú|畉,fú|絥,fú|抚,fǔ|甫,fǔ|府,fǔ|弣,fǔ|拊,fǔ|斧,fǔ|俌,fǔ|郙,fǔ|俯,fǔ|釜,fǔ|釡,fǔ|捬,fǔ|辅,fǔ|椨,fǔ|焤,fǔ|盙,fǔ|腑,fǔ|滏,fǔ|腐,fǔ|輔,fǔ|撫,fǔ|鬴,fǔ|簠,fǔ|黼,fǔ|蚥,fǔ|窗,chuāng|窻,chuāng|傸,chuǎng|创,chuàng|創,chuàng|庄,zhuāng|妝,zhuāng|荘,zhuāng|娤,zhuāng|桩,zhuāng|讣,fù|付,fù|妇,fù|负,fù|附,fù|咐,fù|竎,fù|阜,fù|驸,fù|复,fù|峊,fù|祔,fù|訃,fù|負,fù|赴,fù|袝,fù|偩,fù|冨,fù|副,fù|婦,fù|蚹,fù|傅,fù|媍,fù|富,fù|復,fù|蛗,fù|覄,fù|詂,fù|赋,fù|椱,fù|缚,fù|腹,fù|鲋,fù|禣,fù|褔,fù|赙,fù|緮,fù|蕧,fù|蝜,fù|蝮,fù|賦,fù|駙,fù|縛,fù|鮒,fù|賻,fù|鍑,fù|鍢,fù|鳆,fù|覆,fù|馥,fù|鰒,fù|軵,fù|邚,fù|柎,fù|父,fù|萯,fù|旮,gā|伽,gā|嘎,gā|夾,gā|呷,gā|钆,gá|尜,gá|釓,gá|噶,gá|錷,gá|嘠,gá|尕,gǎ|玍,gǎ|尬,gà|魀,gà|侅,gāi|该,gāi|郂,gāi|陔,gāi|垓,gāi|姟,gāi|峐,gāi|荄,gāi|晐,gāi|赅,gāi|畡,gāi|祴,gāi|絯,gāi|該,gāi|豥,gāi|賅,gāi|賌,gāi|忋,gǎi|改,gǎi|鎅,gǎi|絠,gǎi|丐,gài|乢,gài|匃,gài|匄,gài|钙,gài|盖,gài|摡,gài|溉,gài|葢,gài|鈣,gài|戤,gài|概,gài|蓋,gài|槩,gài|槪,gài|漑,gài|瓂,gài|甘,gān|忓,gān|芉,gān|迀,gān|攼,gān|玕,gān|肝,gān|坩,gān|泔,gān|柑,gān|竿,gān|疳,gān|酐,gān|粓,gān|亁,gān|凲,gān|尲,gān|尴,gān|筸,gān|漧,gān|鳱,gān|尶,gān|尷,gān|魐,gān|矸,gān|虷,gān|釬,gān|乹,gān|諴,gān|飦,gān|苷,gān|杆,gǎn|仠,gǎn|皯,gǎn|秆,gǎn|衦,gǎn|赶,gǎn|敢,gǎn|桿,gǎn|笴,gǎn|稈,gǎn|感,gǎn|澉,gǎn|趕,gǎn|橄,gǎn|擀,gǎn|簳,gǎn|鱤,gǎn|篢,gǎn|豃,gǎn|扞,gǎn|鰔,gǎn|扜,gǎn|鳡,gǎn|干,gàn|旰,gàn|汵,gàn|盰,gàn|绀,gàn|倝,gàn|凎,gàn|淦,gàn|紺,gàn|詌,gàn|骭,gàn|幹,gàn|檊,gàn|赣,gàn|贛,gàn|灨,gàn|贑,gàn|佄,gàn|錎,gàn|棡,gang|冈,gāng|罓,gāng|冮,gāng|刚,gāng|阬,gāng|纲,gāng|肛,gāng|岡,gāng|牨,gāng|疘,gāng|矼,gāng|钢,gāng|剛,gāng|罡,gāng|堈,gāng|釭,gāng|犅,gāng|堽,gāng|綱,gāng|罁,gāng|鋼,gāng|鎠,gāng|頏,gāng|缸,gāng|岗,gǎng|崗,gǎng|港,gǎng|犺,gǎng|掆,gàng|杠,gàng|焵,gàng|筻,gàng|槓,gàng|戆,gàng|戇,gàng|戅,gàng|皋,gāo|羔,gāo|高,gāo|皐,gāo|髙,gāo|臯,gāo|滜,gāo|睾,gāo|膏,gāo|槹,gāo|橰,gāo|篙,gāo|糕,gāo|餻,gāo|櫜,gāo|韟,gāo|鷎,gāo|鼛,gāo|鷱,gāo|獋,gāo|槔,gāo|夰,gǎo|杲,gǎo|菒,gǎo|稁,gǎo|搞,gǎo|缟,gǎo|槀,gǎo|槁,gǎo|獔,gǎo|稾,gǎo|稿,gǎo|镐,gǎo|縞,gǎo|藁,gǎo|檺,gǎo|藳,gǎo|鎬,gǎo|筶,gǎo|澔,gǎo|吿,gào|勂,gào|诰,gào|郜,gào|峼,gào|祮,gào|祰,gào|锆,gào|暠,gào|禞,gào|誥,gào|鋯,gào|告,gào|戈,gē|圪,gē|犵,gē|纥,gē|戓,gē|肐,gē|牫,gē|疙,gē|牱,gē|紇,gē|哥,gē|胳,gē|袼,gē|鸽,gē|割,gē|搁,gē|彁,gē|歌,gē|戨,gē|鴐,gē|鴚,gē|擱,gē|謌,gē|鴿,gē|鎶,gē|咯,gē|滒,gē|杚,gé|呄,gé|匌,gé|挌,gé|阁,gé|革,gé|敋,gé|格,gé|鬲,gé|愅,gé|臵,gé|裓,gé|隔,gé|嗝,gé|塥,gé|滆,gé|觡,gé|搿,gé|膈,gé|閣,gé|镉,gé|鞈,gé|韐,gé|骼,gé|諽,gé|鮯,gé|櫊,gé|鎘,gé|韚,gé|轕,gé|鞷,gé|騔,gé|秴,gé|詥,gé|佫,gé|嘅,gé|猲,gé|槅,gé|閤,gě|葛,gě|哿,gě|舸,gě|鲄,gě|个,gè|各,gè|虼,gè|個,gè|硌,gè|铬,gè|箇,gè|鉻,gè|獦,gè|吤,gè|给,gěi|給,gěi|根,gēn|跟,gēn|哏,gén|亘,gèn|艮,gèn|茛,gèn|揯,gèn|搄,gèn|亙,gèn|刯,gēng|庚,gēng|畊,gēng|浭,gēng|耕,gēng|掶,gēng|菮,gēng|椩,gēng|焿,gēng|絚,gēng|赓,gēng|鹒,gēng|緪,gēng|縆,gēng|賡,gēng|羹,gēng|鶊,gēng|絙,gēng|郠,gěng|哽,gěng|埂,gěng|峺,gěng|挭,gěng|耿,gěng|莄,gěng|梗,gěng|鲠,gěng|骾,gěng|鯁,gěng|郉,gěng|绠,gěng|更,gèng|堩,gèng|暅,gèng|啹,geu|喼,gib|嗰,go|工,gōng|弓,gōng|公,gōng|厷,gōng|功,gōng|攻,gōng|杛,gōng|糼,gōng|肱,gōng|宫,gōng|宮,gōng|恭,gōng|蚣,gōng|躬,gōng|龚,gōng|匑,gōng|塨,gōng|愩,gōng|觥,gōng|躳,gōng|匔,gōng|碽,gōng|髸,gōng|觵,gōng|龔,gōng|魟,gōng|幊,gōng|巩,gǒng|汞,gǒng|拱,gǒng|唝,gǒng|拲,gǒng|栱,gǒng|珙,gǒng|輁,gǒng|鞏,gǒng|嗊,gǒng|銾,gǒng|供,gòng|共,gòng|贡,gòng|羾,gòng|貢,gòng|慐,gòng|熕,gòng|渱,gòng|勾,gōu|沟,gōu|钩,gōu|袧,gōu|缑,gōu|鈎,gōu|溝,gōu|鉤,gōu|緱,gōu|褠,gōu|篝,gōu|簼,gōu|鞲,gōu|冓,gōu|搆,gōu|抅,gōu|泃,gōu|軥,gōu|鴝,gōu|鸜,gōu|佝,gōu|岣,gǒu|狗,gǒu|苟,gǒu|枸,gǒu|玽,gǒu|耇,gǒu|耉,gǒu|笱,gǒu|耈,gǒu|蚼,gǒu|豿,gǒu|坸,gòu|构,gòu|诟,gòu|购,gòu|垢,gòu|姤,gòu|够,gòu|夠,gòu|訽,gòu|媾,gòu|彀,gòu|詬,gòu|遘,gòu|雊,gòu|構,gòu|煹,gòu|觏,gòu|撀,gòu|覯,gòu|購,gòu|傋,gòu|茩,gòu|估,gū|咕,gū|姑,gū|孤,gū|沽,gū|泒,gū|柧,gū|轱,gū|唂,gū|唃,gū|罛,gū|鸪,gū|笟,gū|菇,gū|蛄,gū|蓇,gū|觚,gū|軱,gū|軲,gū|辜,gū|酤,gū|毂,gū|箍,gū|箛,gū|嫴,gū|篐,gū|橭,gū|鮕,gū|鴣,gū|轂,gū|苽,gū|菰,gū|鶻,gú|鹘,gǔ|古,gǔ|扢,gǔ|汩,gǔ|诂,gǔ|谷,gǔ|股,gǔ|峠,gǔ|牯,gǔ|骨,gǔ|罟,gǔ|逧,gǔ|钴,gǔ|傦,gǔ|啒,gǔ|淈,gǔ|脵,gǔ|蛊,gǔ|蛌,gǔ|尳,gǔ|愲,gǔ|焸,gǔ|硲,gǔ|詁,gǔ|馉,gǔ|榾,gǔ|鈷,gǔ|鼓,gǔ|鼔,gǔ|嘏,gǔ|榖,gǔ|皷,gǔ|縎,gǔ|糓,gǔ|薣,gǔ|濲,gǔ|臌,gǔ|餶,gǔ|瀔,gǔ|瞽,gǔ|抇,gǔ|嗀,gǔ|羖,gǔ|固,gù|怘,gù|故,gù|凅,gù|顾,gù|堌,gù|崓,gù|崮,gù|梏,gù|牿,gù|棝,gù|祻,gù|雇,gù|痼,gù|稒,gù|锢,gù|頋,gù|僱,gù|錮,gù|鲴,gù|鯝,gù|顧,gù|盬,gù|瓜,guā|刮,guā|胍,guā|鸹,guā|焻,guā|煱,guā|颪,guā|趏,guā|劀,guā|緺,guā|銽,guā|鴰,guā|騧,guā|呱,guā|諣,guā|栝,guā|歄,guā|冎,guǎ|叧,guǎ|剐,guǎ|剮,guǎ|啩,guǎ|寡,guǎ|卦,guà|坬,guà|诖,guà|挂,guà|掛,guà|罣,guà|絓,guà|罫,guà|褂,guà|詿,guà|乖,guāi|拐,guǎi|枴,guǎi|柺,guǎi|夬,guài|叏,guài|怪,guài|恠,guài|关,guān|观,guān|官,guān|覌,guān|倌,guān|萖,guān|棺,guān|蒄,guān|窤,guān|瘝,guān|癏,guān|観,guān|鳏,guān|關,guān|鰥,guān|觀,guān|鱞,guān|馆,guǎn|痯,guǎn|筦,guǎn|管,guǎn|舘,guǎn|錧,guǎn|館,guǎn|躀,guǎn|鳤,guǎn|輨,guǎn|冠,guàn|卝,guàn|毌,guàn|丱,guàn|贯,guàn|泴,guàn|悺,guàn|惯,guàn|掼,guàn|涫,guàn|貫,guàn|悹,guàn|祼,guàn|慣,guàn|摜,guàn|潅,guàn|遦,guàn|樌,guàn|盥,guàn|罆,guàn|雚,guàn|鏆,guàn|灌,guàn|爟,guàn|瓘,guàn|矔,guàn|鹳,guàn|罐,guàn|鑵,guàn|鸛,guàn|鱹,guàn|懽,guàn|礶,guàn|光,guāng|灮,guāng|侊,guāng|炗,guāng|炚,guāng|炛,guāng|咣,guāng|垙,guāng|姯,guāng|洸,guāng|茪,guāng|桄,guāng|烡,guāng|珖,guāng|胱,guāng|硄,guāng|僙,guāng|輄,guāng|銧,guāng|黆,guāng|欟,guāng|趪,guāng|挄,guāng|广,guǎng|広,guǎng|犷,guǎng|廣,guǎng|臩,guǎng|獷,guǎng|俇,guàng|逛,guàng|臦,guàng|撗,guàng|櫎,guàng|归,guī|圭,guī|妫,guī|龟,guī|规,guī|邽,guī|皈,guī|茥,guī|闺,guī|帰,guī|珪,guī|胿,guī|亀,guī|硅,guī|袿,guī|規,guī|椝,guī|瑰,guī|郌,guī|嫢,guī|摫,guī|閨,guī|鲑,guī|嶲,guī|槻,guī|槼,guī|璝,guī|瞡,guī|膭,guī|鮭,guī|龜,guī|巂,guī|歸,guī|鬶,guī|瓌,guī|鬹,guī|櫷,guī|佹,guī|櫰,guī|螝,guī|槣,guī|鴂,guī|鴃,guī|傀,guī|潙,guī|雟,guī|嬀,guī|宄,guǐ|氿,guǐ|轨,guǐ|庋,guǐ|匦,guǐ|诡,guǐ|陒,guǐ|垝,guǐ|癸,guǐ|軌,guǐ|鬼,guǐ|庪,guǐ|匭,guǐ|晷,guǐ|湀,guǐ|蛫,guǐ|觤,guǐ|詭,guǐ|厬,guǐ|簋,guǐ|蟡,guǐ|攱,guǐ|朹,guǐ|祪,guǐ|猤,guì|媯,guì|刽,guì|刿,guì|攰,guì|昋,guì|柜,guì|炅,guì|贵,guì|桂,guì|椢,guì|筀,guì|貴,guì|蓕,guì|跪,guì|瞆,guì|劊,guì|劌,guì|撌,guì|槶,guì|瞶,guì|櫃,guì|襘,guì|鳜,guì|鞼,guì|鱖,guì|鱥,guì|桧,guì|絵,guì|檜,guì|赽,guì|趹,guì|嶡,guì|禬,guì|衮,gǔn|惃,gǔn|绲,gǔn|袞,gǔn|辊,gǔn|滚,gǔn|蓘,gǔn|滾,gǔn|緄,gǔn|蔉,gǔn|磙,gǔn|輥,gǔn|鲧,gǔn|鮌,gǔn|鯀,gǔn|琯,gùn|棍,gùn|棞,gùn|睔,gùn|睴,gùn|璭,gùn|謴,gùn|呙,guō|埚,guō|郭,guō|啯,guō|崞,guō|楇,guō|聒,guō|鈛,guō|锅,guō|墎,guō|瘑,guō|嘓,guō|彉,guō|蝈,guō|鍋,guō|彍,guō|鐹,guō|矌,guō|簂,guó|囯,guó|囶,guó|囻,guó|国,guó|圀,guó|國,guó|帼,guó|掴,guó|腘,guó|幗,guó|摑,guó|漍,guó|聝,guó|蔮,guó|膕,guó|虢,guó|馘,guó|慖,guó|果,guǒ|惈,guǒ|淉,guǒ|猓,guǒ|菓,guǒ|馃,guǒ|椁,guǒ|褁,guǒ|槨,guǒ|粿,guǒ|綶,guǒ|蜾,guǒ|裹,guǒ|輠,guǒ|餜,guǒ|錁,guǒ|过,guò|過,guò|妎,hā|铪,hā|鉿,hā|哈,hā|蛤,há|孩,hái|骸,hái|還,hái|还,hái|海,hǎi|胲,hǎi|烸,hǎi|塰,hǎi|酼,hǎi|醢,hǎi|亥,hài|骇,hài|害,hài|氦,hài|嗐,hài|餀,hài|駭,hài|駴,hài|嚡,hài|饚,hài|乤,hal|兯,han|爳,han|顸,hān|哻,hān|蚶,hān|酣,hān|谽,hān|馠,hān|魽,hān|鼾,hān|欦,hān|憨,hān|榦,hán|邗,hán|含,hán|邯,hán|函,hán|咁,hán|肣,hán|凾,hán|唅,hán|圅,hán|娢,hán|浛,hán|崡,hán|晗,hán|梒,hán|涵,hán|焓,hán|寒,hán|嵅,hán|韩,hán|甝,hán|筨,hán|蜬,hán|澏,hán|鋡,hán|韓,hán|馯,hán|椷,hán|罕,hǎn|浫,hǎn|喊,hǎn|蔊,hǎn|鬫,hǎn|糮,hǎn|厈,hǎn|汉,hàn|汗,hàn|旱,hàn|悍,hàn|捍,hàn|晘,hàn|涆,hàn|猂,hàn|莟,hàn|晥,hàn|焊,hàn|琀,hàn|菡,hàn|皔,hàn|睅,hàn|傼,hàn|蛿,hàn|撖,hàn|漢,hàn|蜭,hàn|暵,hàn|熯,hàn|銲,hàn|鋎,hàn|憾,hàn|撼,hàn|翰,hàn|螒,hàn|頷,hàn|顄,hàn|駻,hàn|譀,hàn|雗,hàn|瀚,hàn|鶾,hàn|澣,hàn|颔,hàn|魧,hāng|苀,háng|迒,háng|斻,háng|杭,háng|垳,háng|绗,háng|笐,háng|蚢,háng|颃,háng|貥,háng|筕,háng|絎,háng|行,háng|航,háng|沆,hàng|茠,hāo|蒿,hāo|嚆,hāo|薅,hāo|竓,háo|蚝,háo|毫,háo|椃,háo|嗥,háo|獆,háo|噑,háo|豪,háo|嘷,háo|儫,háo|曍,háo|嚎,háo|壕,háo|濠,háo|籇,háo|蠔,háo|譹,háo|虠,háo|諕,háo|呺,háo|郝,hǎo|好,hǎo|号,hào|昊,hào|昦,hào|哠,hào|恏,hào|悎,hào|浩,hào|耗,hào|晧,hào|淏,hào|傐,hào|皓,hào|滈,hào|聕,hào|號,hào|暤,hào|暭,hào|皜,hào|皞,hào|皡,hào|薃,hào|皥,hào|颢,hào|灏,hào|顥,hào|鰝,hào|灝,hào|鄗,hào|藃,hào|诃,hē|呵,hē|抲,hē|欱,hē|喝,hē|訶,hē|嗬,hē|蠚,hē|禾,hé|合,hé|何,hé|劾,hé|咊,hé|和,hé|姀,hé|河,hé|峆,hé|曷,hé|柇,hé|盇,hé|籺,hé|阂,hé|饸,hé|哬,hé|敆,hé|核,hé|盉,hé|盍,hé|啝,hé|涸,hé|渮,hé|盒,hé|菏,hé|萂,hé|龁,hé|惒,hé|粭,hé|訸,hé|颌,hé|楁,hé|鉌,hé|阖,hé|熆,hé|鹖,hé|麧,hé|澕,hé|頜,hé|篕,hé|翮,hé|螛,hé|礉,hé|闔,hé|鞨,hé|齕,hé|覈,hé|鶡,hé|皬,hé|鑉,hé|龢,hé|餄,hé|荷,hé|魺,hé|垎,hè|贺,hè|隺,hè|寉,hè|焃,hè|湼,hè|賀,hè|嗃,hè|煂,hè|碋,hè|熇,hè|褐,hè|赫,hè|鹤,hè|翯,hè|壑,hè|癋,hè|燺,hè|爀,hè|靍,hè|靎,hè|鸖,hè|靏,hè|鶮,hè|謞,hè|鶴,hè|嗨,hēi|黒,hēi|黑,hēi|嘿,hēi|潶,hēi|嬒,hèi|噷,hēn|拫,hén|痕,hén|鞎,hén|佷,hěn|很,hěn|狠,hěn|詪,hěn|恨,hèn|亨,hēng|哼,hēng|悙,hēng|涥,hēng|脝,hēng|姮,héng|恆,héng|恒,héng|桁,héng|烆,héng|珩,héng|胻,héng|横,héng|橫,héng|衡,héng|鴴,héng|鵆,héng|蘅,héng|鑅,héng|鸻,héng|堼,hèng|叿,hōng|灴,hōng|轰,hōng|訇,hōng|烘,hōng|軣,hōng|揈,hōng|渹,hōng|焢,hōng|硡,hōng|薨,hōng|輷,hōng|嚝,hōng|鍧,hōng|轟,hōng|仜,hóng|妅,hóng|红,hóng|吰,hóng|宏,hóng|汯,hóng|玒,hóng|纮,hóng|闳,hóng|宖,hóng|泓,hóng|玜,hóng|苰,hóng|垬,hóng|娂,hóng|洪,hóng|竑,hóng|紅,hóng|荭,hóng|虹,hóng|浤,hóng|紘,hóng|翃,hóng|耾,hóng|硔,hóng|紭,hóng|谹,hóng|鸿,hóng|竤,hóng|粠,hóng|葓,hóng|鈜,hóng|閎,hóng|綋,hóng|翝,hóng|谼,hóng|潂,hóng|鉷,hóng|鞃,hóng|篊,hóng|鋐,hóng|彋,hóng|蕻,hóng|霐,hóng|黉,hóng|霟,hóng|鴻,hóng|黌,hóng|舼,hóng|瓨,hóng|弘,hóng|葒,hóng|哄,hǒng|晎,hǒng|讧,hòng|訌,hòng|閧,hòng|撔,hòng|澋,hòng|澒,hòng|闀,hòng|闂,hòng|腄,hóu|侯,hóu|矦,hóu|喉,hóu|帿,hóu|猴,hóu|葔,hóu|瘊,hóu|睺,hóu|銗,hóu|篌,hóu|糇,hóu|翭,hóu|骺,hóu|鍭,hóu|餱,hóu|鯸,hóu|翵,hóu|吼,hǒu|犼,hǒu|呴,hǒu|后,hòu|郈,hòu|厚,hòu|垕,hòu|後,hòu|洉,hòu|逅,hòu|候,hòu|鄇,hòu|堠,hòu|鲎,hòu|鲘,hòu|鮜,hòu|鱟,hòu|豞,hòu|鋘,hu|乎,hū|匢,hū|呼,hū|垀,hū|忽,hū|昒,hū|曶,hū|泘,hū|苸,hū|烀,hū|轷,hū|匫,hū|唿,hū|惚,hū|淴,hū|虖,hū|軤,hū|雽,hū|嘑,hū|寣,hū|滹,hū|雐,hū|歑,hū|謼,hū|芔,hū|戯,hū|戱,hū|鹄,hú|鵠,hú|囫,hú|弧,hú|狐,hú|瓳,hú|胡,hú|壶,hú|壷,hú|斛,hú|焀,hú|喖,hú|壺,hú|媩,hú|湖,hú|猢,hú|絗,hú|葫,hú|楜,hú|煳,hú|瑚,hú|嘝,hú|蔛,hú|鹕,hú|槲,hú|箶,hú|糊,hú|蝴,hú|衚,hú|縠,hú|螜,hú|醐,hú|頶,hú|觳,hú|鍸,hú|餬,hú|瀫,hú|鬍,hú|鰗,hú|鶘,hú|鶦,hú|沍,hú|礐,hú|瓡,hú|俿,hǔ|虍,hǔ|乕,hǔ|汻,hǔ|虎,hǔ|浒,hǔ|唬,hǔ|萀,hǔ|琥,hǔ|虝,hǔ|滸,hǔ|箎,hǔ|錿,hǔ|鯱,hǔ|互,hù|弖,hù|戶,hù|户,hù|戸,hù|冴,hù|芐,hù|帍,hù|护,hù|沪,hù|岵,hù|怙,hù|戽,hù|昈,hù|枑,hù|祜,hù|笏,hù|粐,hù|婟,hù|扈,hù|瓠,hù|綔,hù|鄠,hù|嫭,hù|嫮,hù|摢,hù|滬,hù|蔰,hù|槴,hù|熩,hù|鳸,hù|簄,hù|鍙,hù|護,hù|鳠,hù|韄,hù|頀,hù|鱯,hù|鸌,hù|濩,hù|穫,hù|觷,hù|魱,hù|冱,hù|鹱,hù|花,huā|芲,huā|埖,huā|婲,huā|椛,huā|硴,huā|糀,huā|誮,huā|錵,huā|蘤,huā|蕐,huā|砉,huā|华,huá|哗,huá|姡,huá|骅,huá|華,huá|铧,huá|滑,huá|猾,huá|嘩,huá|撶,huá|璍,huá|螖,huá|鏵,huá|驊,huá|鷨,huá|划,huá|化,huà|杹,huà|画,huà|话,huà|崋,huà|桦,huà|婳,huà|畫,huà|嬅,huà|畵,huà|觟,huà|話,huà|劃,huà|摦,huà|槬,huà|樺,huà|嫿,huà|澅,huà|諙,huà|黊,huà|繣,huà|舙,huà|蘳,huà|譮,huà|檴,huà|怀,huái|淮,huái|槐,huái|褢,huái|踝,huái|懐,huái|褱,huái|懷,huái|耲,huái|蘹,huái|佪,huái|徊,huái|坏,huài|咶,huài|壊,huài|壞,huài|蘾,huài|欢,huān|歓,huān|鴅,huān|懁,huān|鵍,huān|酄,huān|嚾,huān|獾,huān|歡,huān|貛,huān|讙,huān|驩,huān|貆,huān|环,huán|峘,huán|洹,huán|狟,huán|荁,huán|桓,huán|萈,huán|萑,huán|堚,huán|寏,huán|雈,huán|綄,huán|羦,huán|锾,huán|阛,huán|寰,huán|澴,huán|缳,huán|環,huán|豲,huán|鍰,huán|镮,huán|鹮,huán|糫,huán|繯,huán|轘,huán|鐶,huán|鬟,huán|瞏,huán|鉮,huán|圜,huán|闤,huán|睆,huǎn|缓,huǎn|緩,huǎn|攌,huǎn|幻,huàn|奂,huàn|肒,huàn|奐,huàn|宦,huàn|唤,huàn|换,huàn|浣,huàn|涣,huàn|烉,huàn|患,huàn|梙,huàn|焕,huàn|逭,huàn|喚,huàn|嵈,huàn|愌,huàn|換,huàn|渙,huàn|痪,huàn|煥,huàn|豢,huàn|漶,huàn|瘓,huàn|槵,huàn|鲩,huàn|擐,huàn|瞣,huàn|藧,huàn|鯇,huàn|鯶,huàn|鰀,huàn|圂,huàn|蠸,huàn|瑍,huàn|巟,huāng|肓,huāng|荒,huāng|衁,huāng|塃,huāng|慌,huāng|皇,huáng|偟,huáng|凰,huáng|隍,huáng|黃,huáng|黄,huáng|喤,huáng|堭,huáng|媓,huáng|崲,huáng|徨,huáng|湟,huáng|葟,huáng|遑,huáng|楻,huáng|煌,huáng|瑝,huáng|墴,huáng|潢,huáng|獚,huáng|锽,huáng|熿,huáng|璜,huáng|篁,huáng|艎,huáng|蝗,huáng|癀,huáng|磺,huáng|穔,huáng|諻,huáng|簧,huáng|蟥,huáng|鍠,huáng|餭,huáng|鳇,huáng|鐄,huáng|騜,huáng|鰉,huáng|鷬,huáng|惶,huáng|鱑,huáng|怳,huǎng|恍,huǎng|炾,huǎng|宺,huǎng|晃,huǎng|晄,huǎng|奛,huǎng|谎,huǎng|幌,huǎng|愰,huǎng|詤,huǎng|縨,huǎng|謊,huǎng|皩,huǎng|兤,huǎng|滉,huàng|榥,huàng|曂,huàng|皝,huàng|鎤,huàng|鰴,hui|灰,huī|灳,huī|诙,huī|咴,huī|恢,huī|拻,huī|挥,huī|虺,huī|晖,huī|烣,huī|珲,huī|豗,huī|婎,huī|媈,huī|揮,huī|翚,huī|辉,huī|暉,huī|楎,huī|琿,huī|禈,huī|詼,huī|幑,huī|睳,huī|噅,huī|噕,huī|翬,huī|輝,huī|麾,huī|徽,huī|隳,huī|瀈,huī|洃,huī|煇,huí|囘,huí|回,huí|囬,huí|廻,huí|廽,huí|恛,huí|洄,huí|茴,huí|迴,huí|烠,huí|逥,huí|痐,huí|蛔,huí|蛕,huí|蜖,huí|鮰,huí|藱,huí|悔,huǐ|毇,huǐ|檓,huǐ|燬,huǐ|譭,huǐ|泋,huǐ|毁,huǐ|烜,huǐ|卉,huì|屷,huì|汇,huì|会,huì|讳,huì|浍,huì|绘,huì|荟,huì|诲,huì|恚,huì|恵,huì|烩,huì|贿,huì|彗,huì|晦,huì|秽,huì|喙,huì|惠,huì|缋,huì|翙,huì|阓,huì|匯,huì|彙,huì|彚,huì|會,huì|毀,huì|滙,huì|詯,huì|賄,huì|嘒,huì|蔧,huì|誨,huì|圚,huì|寭,huì|慧,huì|憓,huì|暳,huì|槥,huì|潓,huì|蕙,huì|徻,huì|橞,huì|澮,huì|獩,huì|璤,huì|薈,huì|薉,huì|諱,huì|檅,huì|燴,huì|篲,huì|餯,huì|嚖,huì|瞺,huì|穢,huì|繢,huì|蟪,huì|櫘,huì|繪,huì|翽,huì|譓,huì|儶,huì|鏸,huì|闠,huì|孈,huì|鐬,huì|靧,huì|韢,huì|譿,huì|顪,huì|銊,huì|叀,huì|僡,huì|懳,huì|昏,hūn|昬,hūn|荤,hūn|婚,hūn|惛,hūn|涽,hūn|阍,hūn|惽,hūn|棔,hūn|葷,hūn|睧,hūn|閽,hūn|焄,hūn|蔒,hūn|睯,hūn|忶,hún|浑,hún|馄,hún|渾,hún|魂,hún|餛,hún|繉,hún|轋,hún|鼲,hún|混,hún|梱,hún|湷,hún|诨,hùn|俒,hùn|倱,hùn|掍,hùn|焝,hùn|溷,hùn|慁,hùn|觨,hùn|諢,hùn|吙,huō|耠,huō|锪,huō|劐,huō|鍃,huō|豁,huō|攉,huō|騞,huō|搉,huō|佸,huó|秮,huó|活,huó|火,huǒ|伙,huǒ|邩,huǒ|钬,huǒ|鈥,huǒ|夥,huǒ|沎,huò|或,huò|货,huò|咟,huò|俰,huò|捇,huò|眓,huò|获,huò|閄,huò|剨,huò|掝,huò|祸,huò|貨,huò|惑,huò|旤,huò|湱,huò|禍,huò|奯,huò|獲,huò|霍,huò|謋,huò|镬,huò|嚯,huò|瀖,huò|耯,huò|藿,huò|蠖,huò|嚿,huò|曤,huò|臛,huò|癨,huò|矐,huò|鑊,huò|靃,huò|謔,huò|篧,huò|擭,huò|夻,hwa|丌,jī|讥,jī|击,jī|刉,jī|叽,jī|饥,jī|乩,jī|圾,jī|机,jī|玑,jī|肌,jī|芨,jī|矶,jī|鸡,jī|枅,jī|咭,jī|剞,jī|唧,jī|姬,jī|屐,jī|积,jī|笄,jī|飢,jī|基,jī|喞,jī|嵆,jī|嵇,jī|攲,jī|敧,jī|犄,jī|筓,jī|缉,jī|赍,jī|嗘,jī|稘,jī|跻,jī|鳮,jī|僟,jī|毄,jī|箕,jī|銈,jī|嘰,jī|撃,jī|樭,jī|畿,jī|稽,jī|緝,jī|觭,jī|賫,jī|躸,jī|齑,jī|墼,jī|憿,jī|機,jī|激,jī|璣,jī|禨,jī|積,jī|錤,jī|隮,jī|擊,jī|磯,jī|簊,jī|羁,jī|賷,jī|鄿,jī|櫅,jī|耭,jī|雞,jī|譏,jī|韲,jī|鶏,jī|譤,jī|鐖,jī|癪,jī|躋,jī|鞿,jī|鷄,jī|齎,jī|羇,jī|虀,jī|鑇,jī|覉,jī|鑙,jī|齏,jī|羈,jī|鸄,jī|覊,jī|庴,jī|垍,jī|諅,jī|踦,jī|璂,jī|踑,jī|谿,jī|刏,jī|畸,jī|簎,jí|諔,jí|堲,jí|蠀,jí|亼,jí|及,jí|吉,jí|彶,jí|忣,jí|汲,jí|级,jí|即,jí|极,jí|亟,jí|佶,jí|郆,jí|卽,jí|叝,jí|姞,jí|急,jí|狤,jí|皍,jí|笈,jí|級,jí|揤,jí|疾,jí|觙,jí|偮,jí|卙,jí|楖,jí|焏,jí|脨,jí|谻,jí|戢,jí|棘,jí|極,jí|湒,jí|集,jí|塉,jí|嫉,jí|愱,jí|楫,jí|蒺,jí|蝍,jí|趌,jí|辑,jí|槉,jí|耤,jí|膌,jí|銡,jí|嶯,jí|潗,jí|瘠,jí|箿,jí|蕀,jí|蕺,jí|踖,jí|鞊,jí|鹡,jí|橶,jí|檝,jí|濈,jí|螏,jí|輯,jí|襋,jí|蹐,jí|艥,jí|籍,jí|轚,jí|鏶,jí|霵,jí|鶺,jí|鷑,jí|躤,jí|雦,jí|雧,jí|嵴,jí|尐,jí|淁,jí|吇,jí|莋,jí|岌,jí|殛,jí|鍓,jí|颳,jǐ|几,jǐ|己,jǐ|丮,jǐ|妀,jǐ|犱,jǐ|泲,jǐ|虮,jǐ|挤,jǐ|脊,jǐ|掎,jǐ|鱾,jǐ|幾,jǐ|戟,jǐ|麂,jǐ|魢,jǐ|撠,jǐ|擠,jǐ|穖,jǐ|蟣,jǐ|済,jǐ|畟,jì|迹,jì|绩,jì|勣,jì|彑,jì|旡,jì|计,jì|记,jì|伎,jì|纪,jì|坖,jì|妓,jì|忌,jì|技,jì|芰,jì|芶,jì|际,jì|剂,jì|季,jì|哜,jì|峜,jì|既,jì|洎,jì|济,jì|紀,jì|茍,jì|計,jì|剤,jì|紒,jì|继,jì|觊,jì|記,jì|偈,jì|寄,jì|徛,jì|悸,jì|旣,jì|梞,jì|祭,jì|萕,jì|惎,jì|臮,jì|葪,jì|蔇,jì|兾,jì|痵,jì|継,jì|蓟,jì|裚,jì|跡,jì|際,jì|墍,jì|暨,jì|漃,jì|漈,jì|禝,jì|稩,jì|穊,jì|誋,jì|跽,jì|霁,jì|鲚,jì|稷,jì|鲫,jì|冀,jì|劑,jì|曁,jì|穄,jì|縘,jì|薊,jì|襀,jì|髻,jì|嚌,jì|檕,jì|濟,jì|繋,jì|罽,jì|覬,jì|鮆,jì|檵,jì|璾,jì|蹟,jì|鯽,jì|鵋,jì|齌,jì|廭,jì|懻,jì|癠,jì|穧,jì|糭,jì|繫,jì|骥,jì|鯚,jì|瀱,jì|繼,jì|蘮,jì|鱀,jì|蘻,jì|霽,jì|鰶,jì|鰿,jì|鱭,jì|驥,jì|訐,jì|魝,jì|櫭,jì|帺,jì|褀,jì|鬾,jì|懠,jì|蟿,jì|汥,jì|鯯,jì|齍,jì|績,jì|寂,jì|暩,jì|蘎,jì|筴,jiā|加,jiā|抸,jiā|佳,jiā|泇,jiā|迦,jiā|枷,jiā|毠,jiā|浃,jiā|珈,jiā|埉,jiā|家,jiā|浹,jiā|痂,jiā|梜,jiā|耞,jiā|袈,jiā|猳,jiā|葭,jiā|跏,jiā|犌,jiā|腵,jiā|鉫,jiā|嘉,jiā|镓,jiā|糘,jiā|豭,jiā|貑,jiā|鎵,jiā|麚,jiā|椵,jiā|挟,jiā|挾,jiā|笳,jiā|夹,jiá|袷,jiá|裌,jiá|圿,jiá|扴,jiá|郏,jiá|荚,jiá|郟,jiá|唊,jiá|恝,jiá|莢,jiá|戛,jiá|脥,jiá|铗,jiá|蛱,jiá|颊,jiá|蛺,jiá|跲,jiá|鋏,jiá|頬,jiá|頰,jiá|鴶,jiá|鵊,jiá|忦,jiá|戞,jiá|岬,jiǎ|甲,jiǎ|叚,jiǎ|玾,jiǎ|胛,jiǎ|斚,jiǎ|贾,jiǎ|钾,jiǎ|婽,jiǎ|徦,jiǎ|斝,jiǎ|賈,jiǎ|鉀,jiǎ|榎,jiǎ|槚,jiǎ|瘕,jiǎ|檟,jiǎ|夓,jiǎ|假,jiǎ|价,jià|驾,jià|架,jià|嫁,jià|幏,jià|榢,jià|價,jià|稼,jià|駕,jià|戋,jiān|奸,jiān|尖,jiān|幵,jiān|坚,jiān|歼,jiān|间,jiān|冿,jiān|戔,jiān|肩,jiān|艰,jiān|姦,jiān|姧,jiān|兼,jiān|监,jiān|堅,jiān|惤,jiān|猏,jiān|笺,jiān|菅,jiān|菺,jiān|牋,jiān|犍,jiān|缄,jiān|葌,jiān|葏,jiān|間,jiān|靬,jiān|搛,jiān|椾,jiān|煎,jiān|瑊,jiān|睷,jiān|碊,jiān|缣,jiān|蒹,jiān|監,jiān|箋,jiān|樫,jiān|熞,jiān|緘,jiān|蕑,jiān|蕳,jiān|鲣,jiān|鳽,jiān|鹣,jiān|熸,jiān|篯,jiān|縑,jiān|艱,jiān|鞬,jiān|餰,jiān|馢,jiān|麉,jiān|瀐,jiān|鞯,jiān|鳒,jiān|殱,jiān|礛,jiān|覸,jiān|鵳,jiān|瀸,jiān|櫼,jiān|殲,jiān|譼,jiān|鰜,jiān|鶼,jiān|籛,jiān|韀,jiān|鰹,jiān|囏,jiān|虃,jiān|鑯,jiān|韉,jiān|揃,jiān|鐗,jiān|鐧,jiān|閒,jiān|黚,jiān|傔,jiān|攕,jiān|纎,jiān|钘,jiān|鈃,jiān|銒,jiān|籈,jiān|湔,jiān|囝,jiǎn|拣,jiǎn|枧,jiǎn|俭,jiǎn|茧,jiǎn|倹,jiǎn|挸,jiǎn|捡,jiǎn|笕,jiǎn|减,jiǎn|剪,jiǎn|帴,jiǎn|梘,jiǎn|检,jiǎn|湕,jiǎn|趼,jiǎn|揀,jiǎn|検,jiǎn|減,jiǎn|睑,jiǎn|硷,jiǎn|裥,jiǎn|詃,jiǎn|锏,jiǎn|弿,jiǎn|瑐,jiǎn|筧,jiǎn|简,jiǎn|絸,jiǎn|谫,jiǎn|彅,jiǎn|戩,jiǎn|碱,jiǎn|儉,jiǎn|翦,jiǎn|撿,jiǎn|檢,jiǎn|藆,jiǎn|襇,jiǎn|襉,jiǎn|謇,jiǎn|蹇,jiǎn|瞼,jiǎn|礆,jiǎn|簡,jiǎn|繭,jiǎn|謭,jiǎn|鬋,jiǎn|鰎,jiǎn|鹸,jiǎn|瀽,jiǎn|蠒,jiǎn|鹻,jiǎn|譾,jiǎn|襺,jiǎn|鹼,jiǎn|堿,jiǎn|偂,jiǎn|銭,jiǎn|醎,jiǎn|鹹,jiǎn|涀,jiǎn|橏,jiǎn|柬,jiǎn|戬,jiǎn|见,jiàn|件,jiàn|見,jiàn|侟,jiàn|饯,jiàn|剑,jiàn|洊,jiàn|牮,jiàn|荐,jiàn|贱,jiàn|俴,jiàn|健,jiàn|剣,jiàn|栫,jiàn|涧,jiàn|珔,jiàn|舰,jiàn|剱,jiàn|徤,jiàn|渐,jiàn|袸,jiàn|谏,jiàn|釼,jiàn|寋,jiàn|旔,jiàn|楗,jiàn|毽,jiàn|溅,jiàn|腱,jiàn|臶,jiàn|葥,jiàn|践,jiàn|鉴,jiàn|键,jiàn|僭,jiàn|榗,jiàn|漸,jiàn|劍,jiàn|劎,jiàn|墹,jiàn|澗,jiàn|箭,jiàn|糋,jiàn|諓,jiàn|賤,jiàn|趝,jiàn|踐,jiàn|踺,jiàn|劒,jiàn|劔,jiàn|橺,jiàn|薦,jiàn|諫,jiàn|鍵,jiàn|餞,jiàn|瞯,jiàn|瞷,jiàn|磵,jiàn|礀,jiàn|螹,jiàn|鍳,jiàn|濺,jiàn|繝,jiàn|瀳,jiàn|鏩,jiàn|艦,jiàn|轞,jiàn|鑑,jiàn|鑒,jiàn|鑬,jiàn|鑳,jiàn|鐱,jiàn|揵,jiàn|蔪,jiàn|橌,jiàn|廴,jiàn|譖,jiàn|鋻,jiàn|建,jiàn|賎,jiàn|擶,jiàn|江,jiāng|姜,jiāng|将,jiāng|茳,jiāng|浆,jiāng|畕,jiāng|豇,jiāng|葁,jiāng|摪,jiāng|翞,jiāng|僵,jiāng|漿,jiāng|螀,jiāng|壃,jiāng|彊,jiāng|缰,jiāng|薑,jiāng|殭,jiāng|螿,jiāng|鳉,jiāng|疅,jiāng|礓,jiāng|疆,jiāng|繮,jiāng|韁,jiāng|鱂,jiāng|將,jiāng|畺,jiāng|糡,jiāng|橿,jiāng|讲,jiǎng|奖,jiǎng|桨,jiǎng|蒋,jiǎng|勥,jiǎng|奨,jiǎng|奬,jiǎng|蔣,jiǎng|槳,jiǎng|獎,jiǎng|耩,jiǎng|膙,jiǎng|講,jiǎng|顜,jiǎng|塂,jiǎng|匞,jiàng|匠,jiàng|夅,jiàng|弜,jiàng|杢,jiàng|降,jiàng|绛,jiàng|弶,jiàng|袶,jiàng|絳,jiàng|酱,jiàng|摾,jiàng|滰,jiàng|嵹,jiàng|犟,jiàng|醤,jiàng|糨,jiàng|醬,jiàng|櫤,jiàng|謽,jiàng|蔃,jiàng|洚,jiàng|艽,jiāo|芁,jiāo|交,jiāo|郊,jiāo|姣,jiāo|娇,jiāo|峧,jiāo|浇,jiāo|茭,jiāo|骄,jiāo|胶,jiāo|椒,jiāo|焳,jiāo|蛟,jiāo|跤,jiāo|僬,jiāo|嘄,jiāo|鲛,jiāo|嬌,jiāo|嶕,jiāo|嶣,jiāo|憍,jiāo|澆,jiāo|膠,jiāo|蕉,jiāo|燋,jiāo|膲,jiāo|礁,jiāo|穚,jiāo|鮫,jiāo|鹪,jiāo|簥,jiāo|蟭,jiāo|轇,jiāo|鐎,jiāo|驕,jiāo|鷦,jiāo|鷮,jiāo|儌,jiāo|撟,jiāo|挍,jiāo|教,jiāo|骹,jiāo|嫶,jiāo|萩,jiāo|嘐,jiāo|憢,jiāo|焦,jiāo|櫵,jiáo|嚼,jiáo|臫,jiǎo|佼,jiǎo|挢,jiǎo|狡,jiǎo|绞,jiǎo|饺,jiǎo|晈,jiǎo|笅,jiǎo|皎,jiǎo|矫,jiǎo|脚,jiǎo|铰,jiǎo|搅,jiǎo|筊,jiǎo|絞,jiǎo|剿,jiǎo|勦,jiǎo|敫,jiǎo|湬,jiǎo|煍,jiǎo|腳,jiǎo|賋,jiǎo|摷,jiǎo|暞,jiǎo|踋,jiǎo|鉸,jiǎo|劋,jiǎo|撹,jiǎo|徼,jiǎo|敽,jiǎo|敿,jiǎo|缴,jiǎo|曒,jiǎo|璬,jiǎo|矯,jiǎo|皦,jiǎo|蟜,jiǎo|鵤,jiǎo|繳,jiǎo|譑,jiǎo|孂,jiǎo|纐,jiǎo|攪,jiǎo|灚,jiǎo|鱎,jiǎo|潐,jiǎo|糸,jiǎo|蹻,jiǎo|釥,jiǎo|纟,jiǎo|恔,jiǎo|角,jiǎo|餃,jiǎo|叫,jiào|呌,jiào|訆,jiào|珓,jiào|轿,jiào|较,jiào|窖,jiào|滘,jiào|較,jiào|嘂,jiào|嘦,jiào|斠,jiào|漖,jiào|酵,jiào|噍,jiào|噭,jiào|嬓,jiào|獥,jiào|藠,jiào|趭,jiào|轎,jiào|醮,jiào|譥,jiào|皭,jiào|釂,jiào|觉,jiào|覐,jiào|覚,jiào|覺,jiào|趫,jiào|敎,jiào|阶,jiē|疖,jiē|皆,jiē|接,jiē|掲,jiē|痎,jiē|秸,jiē|菨,jiē|喈,jiē|嗟,jiē|堦,jiē|媘,jiē|嫅,jiē|揭,jiē|椄,jiē|湝,jiē|脻,jiē|街,jiē|煯,jiē|稭,jiē|鞂,jiē|蝔,jiē|擑,jiē|癤,jiē|鶛,jiē|节,jiē|節,jiē|袓,jiē|謯,jiē|階,jiē|卪,jié|孑,jié|讦,jié|刦,jié|刧,jié|劫,jié|岊,jié|昅,jié|刼,jié|劼,jié|疌,jié|衱,jié|诘,jié|拮,jié|洁,jié|结,jié|迼,jié|倢,jié|桀,jié|桝,jié|莭,jié|偼,jié|婕,jié|崨,jié|捷,jié|袺,jié|傑,jié|媫,jié|結,jié|蛣,jié|颉,jié|嵥,jié|楬,jié|楶,jié|滐,jié|睫,jié|蜐,jié|詰,jié|截,jié|榤,jié|碣,jié|竭,jié|蓵,jié|鲒,jié|潔,jié|羯,jié|誱,jié|踕,jié|頡,jié|幯,jié|擳,jié|嶻,jié|擮,jié|礍,jié|鍻,jié|鮚,jié|巀,jié|蠞,jié|蠘,jié|蠽,jié|洯,jié|絜,jié|搩,jié|杰,jié|鉣,jié|姐,jiě|毑,jiě|媎,jiě|解,jiě|觧,jiě|檞,jiě|飷,jiě|丯,jiè|介,jiè|岕,jiè|庎,jiè|戒,jiè|芥,jiè|屆,jiè|届,jiè|斺,jiè|玠,jiè|界,jiè|畍,jiè|疥,jiè|砎,jiè|衸,jiè|诫,jiè|借,jiè|蚧,jiè|徣,jiè|堺,jiè|楐,jiè|琾,jiè|蛶,jiè|骱,jiè|犗,jiè|誡,jiè|魪,jiè|藉,jiè|繲,jiè|雃,jiè|嶰,jiè|唶,jiè|褯,jiè|巾,jīn|今,jīn|斤,jīn|钅,jīn|兓,jīn|金,jīn|釒,jīn|津,jīn|矜,jīn|砛,jīn|荕,jīn|衿,jīn|觔,jīn|埐,jīn|珒,jīn|紟,jīn|惍,jīn|琎,jīn|堻,jīn|琻,jīn|筋,jīn|嶜,jīn|璡,jīn|鹶,jīn|黅,jīn|襟,jīn|濜,jīn|仅,jǐn|巹,jǐn|紧,jǐn|堇,jǐn|菫,jǐn|僅,jǐn|厪,jǐn|谨,jǐn|锦,jǐn|嫤,jǐn|廑,jǐn|漌,jǐn|緊,jǐn|蓳,jǐn|馑,jǐn|槿,jǐn|瑾,jǐn|錦,jǐn|謹,jǐn|饉,jǐn|儘,jǐn|婜,jǐn|斳,jǐn|卺,jǐn|笒,jìn|盡,jìn|劤,jìn|尽,jìn|劲,jìn|妗,jìn|近,jìn|进,jìn|侭,jìn|枃,jìn|勁,jìn|荩,jìn|晉,jìn|晋,jìn|浸,jìn|烬,jìn|赆,jìn|祲,jìn|進,jìn|煡,jìn|缙,jìn|寖,jìn|搢,jìn|溍,jìn|禁,jìn|靳,jìn|墐,jìn|慬,jìn|瑨,jìn|僸,jìn|凚,jìn|歏,jìn|殣,jìn|觐,jìn|噤,jìn|濅,jìn|縉,jìn|賮,jìn|嚍,jìn|壗,jìn|藎,jìn|燼,jìn|璶,jìn|覲,jìn|贐,jìn|齽,jìn|馸,jìn|臸,jìn|浕,jìn|嬧,jìn|坕,jīng|坙,jīng|巠,jīng|京,jīng|泾,jīng|经,jīng|茎,jīng|亰,jīng|秔,jīng|荆,jīng|荊,jīng|涇,jīng|莖,jīng|婛,jīng|惊,jīng|旌,jīng|旍,jīng|猄,jīng|経,jīng|菁,jīng|晶,jīng|稉,jīng|腈,jīng|粳,jīng|經,jīng|兢,jīng|精,jīng|聙,jīng|橸,jīng|鲸,jīng|鵛,jīng|鯨,jīng|鶁,jīng|麖,jīng|鼱,jīng|驚,jīng|麠,jīng|徑,jīng|仱,jīng|靑,jīng|睛,jīng|井,jǐng|阱,jǐng|刭,jǐng|坓,jǐng|宑,jǐng|汫,jǐng|汬,jǐng|肼,jǐng|剄,jǐng|穽,jǐng|颈,jǐng|景,jǐng|儆,jǐng|幜,jǐng|璄,jǐng|憼,jǐng|暻,jǐng|燝,jǐng|璟,jǐng|璥,jǐng|頸,jǐng|蟼,jǐng|警,jǐng|擏,jǐng|憬,jǐng|妌,jìng|净,jìng|弪,jìng|径,jìng|迳,jìng|浄,jìng|胫,jìng|凈,jìng|弳,jìng|痉,jìng|竞,jìng|逕,jìng|婙,jìng|婧,jìng|桱,jìng|梷,jìng|淨,jìng|竫,jìng|脛,jìng|敬,jìng|痙,jìng|竧,jìng|傹,jìng|靖,jìng|境,jìng|獍,jìng|誩,jìng|静,jìng|頚,jìng|曔,jìng|镜,jìng|靜,jìng|瀞,jìng|鏡,jìng|競,jìng|竸,jìng|葝,jìng|儬,jìng|陘,jìng|竟,jìng|冋,jiōng|扃,jiōng|埛,jiōng|絅,jiōng|駉,jiōng|駫,jiōng|冏,jiōng|浻,jiōng|扄,jiōng|銄,jiōng|囧,jiǒng|迥,jiǒng|侰,jiǒng|炯,jiǒng|逈,jiǒng|烱,jiǒng|煚,jiǒng|窘,jiǒng|颎,jiǒng|綗,jiǒng|僒,jiǒng|煛,jiǒng|熲,jiǒng|澃,jiǒng|燛,jiǒng|褧,jiǒng|顈,jiǒng|蘔,jiǒng|宭,jiǒng|蘏,jiǒng|丩,jiū|勼,jiū|纠,jiū|朻,jiū|究,jiū|糺,jiū|鸠,jiū|赳,jiū|阄,jiū|萛,jiū|啾,jiū|揪,jiū|揫,jiū|鳩,jiū|摎,jiū|鬏,jiū|鬮,jiū|稵,jiū|糾,jiū|九,jiǔ|久,jiǔ|乆,jiǔ|乣,jiǔ|奺,jiǔ|汣,jiǔ|杦,jiǔ|灸,jiǔ|玖,jiǔ|舏,jiǔ|韭,jiǔ|紤,jiǔ|酒,jiǔ|镹,jiǔ|韮,jiǔ|匛,jiù|旧,jiù|臼,jiù|疚,jiù|柩,jiù|柾,jiù|倃,jiù|桕,jiù|厩,jiù|救,jiù|就,jiù|廄,jiù|匓,jiù|舅,jiù|僦,jiù|廏,jiù|廐,jiù|慦,jiù|殧,jiù|舊,jiù|鹫,jiù|麔,jiù|匶,jiù|齨,jiù|鷲,jiù|咎,jiù|欍,jou|鶪,ju|伡,jū|俥,jū|凥,jū|匊,jū|居,jū|狙,jū|苴,jū|驹,jū|倶,jū|挶,jū|捄,jū|疽,jū|痀,jū|眗,jū|砠,jū|罝,jū|陱,jū|娵,jū|婅,jū|婮,jū|崌,jū|掬,jū|梮,jū|涺,jū|椐,jū|琚,jū|腒,jū|趄,jū|跔,jū|锔,jū|裾,jū|雎,jū|艍,jū|蜛,jū|踘,jū|鋦,jū|駒,jū|鮈,jū|鴡,jū|鞠,jū|鞫,jū|鶋,jū|臄,jū|揟,jū|拘,jū|諊,jū|局,jú|泦,jú|侷,jú|狊,jú|桔,jú|毩,jú|淗,jú|焗,jú|菊,jú|郹,jú|椈,jú|毱,jú|湨,jú|犑,jú|輂,jú|粷,jú|蓻,jú|趜,jú|躹,jú|閰,jú|檋,jú|駶,jú|鵙,jú|蹫,jú|鵴,jú|巈,jú|蘜,jú|鼰,jú|鼳,jú|驧,jú|趉,jú|郥,jú|橘,jú|咀,jǔ|弆,jǔ|沮,jǔ|举,jǔ|矩,jǔ|莒,jǔ|挙,jǔ|椇,jǔ|筥,jǔ|榉,jǔ|榘,jǔ|蒟,jǔ|龃,jǔ|聥,jǔ|舉,jǔ|踽,jǔ|擧,jǔ|櫸,jǔ|齟,jǔ|襷,jǔ|籧,jǔ|郰,jǔ|欅,jǔ|句,jù|巨,jù|讵,jù|姖,jù|岠,jù|怇,jù|拒,jù|洰,jù|苣,jù|邭,jù|具,jù|怚,jù|拠,jù|昛,jù|歫,jù|炬,jù|秬,jù|钜,jù|俱,jù|倨,jù|冣,jù|剧,jù|粔,jù|耟,jù|蚷,jù|埧,jù|埾,jù|惧,jù|詎,jù|距,jù|焣,jù|犋,jù|跙,jù|鉅,jù|飓,jù|虡,jù|豦,jù|锯,jù|愳,jù|窭,jù|聚,jù|駏,jù|劇,jù|勮,jù|屦,jù|踞,jù|鮔,jù|壉,jù|懅,jù|據,jù|澽,jù|遽,jù|鋸,jù|屨,jù|颶,jù|簴,jù|躆,jù|醵,jù|懼,jù|鐻,jù|爠,jù|坥,jù|螶,jù|忂,jù|葅,jù|蒩,jù|珇,jù|据,jù|姢,juān|娟,juān|捐,juān|涓,juān|脧,juān|裐,juān|鹃,juān|勬,juān|鋑,juān|鋗,juān|镌,juān|鎸,juān|鵑,juān|鐫,juān|蠲,juān|勌,juān|瓹,juān|梋,juān|鞙,juān|朘,juān|呟,juǎn|帣,juǎn|埍,juǎn|捲,juǎn|菤,juǎn|锩,juǎn|臇,juǎn|錈,juǎn|埢,juǎn|踡,juǎn|蕋,juǎn|卷,juàn|劵,juàn|弮,juàn|倦,juàn|桊,juàn|狷,juàn|绢,juàn|淃,juàn|眷,juàn|鄄,juàn|睊,juàn|絭,juàn|罥,juàn|睠,juàn|絹,juàn|慻,juàn|蔨,juàn|餋,juàn|獧,juàn|羂,juàn|圏,juàn|棬,juàn|惓,juàn|韏,juàn|讂,juàn|縳,juàn|襈,juàn|奆,juàn|噘,juē|撅,juē|撧,juē|屩,juē|屫,juē|繑,juē|亅,jué|孓,jué|决,jué|刔,jué|氒,jué|诀,jué|抉,jué|決,jué|芵,jué|泬,jué|玦,jué|玨,jué|挗,jué|珏,jué|砄,jué|绝,jué|虳,jué|捔,jué|欮,jué|蚗,jué|崛,jué|掘,jué|斍,jué|桷,jué|殌,jué|焆,jué|觖,jué|逫,jué|傕,jué|厥,jué|絕,jué|絶,jué|鈌,jué|劂,jué|勪,jué|瑴,jué|谲,jué|嶥,jué|憰,jué|潏,jué|熦,jué|爴,jué|獗,jué|瘚,jué|蕝,jué|蕨,jué|憠,jué|橛,jué|镼,jué|爵,jué|镢,jué|蟨,jué|蟩,jué|爑,jué|譎,jué|蹷,jué|鶌,jué|矍,jué|鐝,jué|灍,jué|爝,jué|觼,jué|彏,jué|戄,jué|攫,jué|玃,jué|鷢,jué|欔,jué|矡,jué|龣,jué|貜,jué|躩,jué|钁,jué|璚,jué|匷,jué|啳,jué|吷,jué|疦,jué|弡,jué|穱,jué|孒,jué|訣,jué|橜,jué|蹶,juě|倔,juè|誳,juè|君,jun1|均,jun1|汮,jun1|姰,jun1|袀,jun1|軍,jun1|钧,jun1|莙,jun1|蚐,jun1|桾,jun1|皲,jun1|菌,jun1|鈞,jun1|碅,jun1|筠,jun1|皸,jun1|皹,jun1|覠,jun1|銁,jun1|銞,jun1|鲪,jun1|麇,jun1|鍕,jun1|鮶,jun1|麏,jun1|麕,jun1|军,jun1|隽,jun4|雋,jun4|呁,jun4|俊,jun4|郡,jun4|陖,jun4|峻,jun4|捃,jun4|晙,jun4|馂,jun4|骏,jun4|焌,jun4|珺,jun4|畯,jun4|竣,jun4|箘,jun4|箟,jun4|蜠,jun4|儁,jun4|寯,jun4|懏,jun4|餕,jun4|燇,jun4|駿,jun4|鵔,jun4|鵕,jun4|鵘,jun4|葰,jun4|埈,jun4|咔,kā|咖,kā|喀,kā|衉,kā|哢,kā|呿,kā|卡,kǎ|佧,kǎ|垰,kǎ|裃,kǎ|鉲,kǎ|胩,kǎ|开,kāi|奒,kāi|揩,kāi|锎,kāi|開,kāi|鐦,kāi|凯,kǎi|剀,kǎi|垲,kǎi|恺,kǎi|闿,kǎi|铠,kǎi|凱,kǎi|慨,kǎi|蒈,kǎi|塏,kǎi|愷,kǎi|楷,kǎi|輆,kǎi|暟,kǎi|锴,kǎi|鍇,kǎi|鎧,kǎi|闓,kǎi|颽,kǎi|喫,kài|噄,kài|忾,kài|烗,kài|勓,kài|愾,kài|鎎,kài|愒,kài|欯,kài|炌,kài|乫,kal|刊,kān|栞,kān|勘,kān|龛,kān|堪,kān|嵁,kān|戡,kān|龕,kān|槛,kǎn|檻,kǎn|冚,kǎn|坎,kǎn|侃,kǎn|砍,kǎn|莰,kǎn|偘,kǎn|埳,kǎn|惂,kǎn|欿,kǎn|塪,kǎn|輡,kǎn|竷,kǎn|轗,kǎn|衎,kǎn|看,kàn|崁,kàn|墈,kàn|阚,kàn|瞰,kàn|磡,kàn|闞,kàn|矙,kàn|輱,kàn|忼,kāng|砊,kāng|粇,kāng|康,kāng|嫝,kāng|嵻,kāng|慷,kāng|漮,kāng|槺,kāng|穅,kāng|糠,kāng|躿,kāng|鏮,kāng|鱇,kāng|闶,kāng|閌,kāng|扛,káng|摃,káng|亢,kàng|伉,kàng|匟,kàng|囥,kàng|抗,kàng|炕,kàng|钪,kàng|鈧,kàng|邟,kàng|尻,kāo|髛,kāo|嵪,kāo|訄,kāo|薧,kǎo|攷,kǎo|考,kǎo|拷,kǎo|洘,kǎo|栲,kǎo|烤,kǎo|铐,kào|犒,kào|銬,kào|鲓,kào|靠,kào|鮳,kào|鯌,kào|焅,kào|屙,kē|蚵,kē|苛,kē|柯,kē|牁,kē|珂,kē|胢,kē|轲,kē|疴,kē|趷,kē|钶,kē|嵙,kē|棵,kē|痾,kē|萪,kē|軻,kē|颏,kē|犐,kē|稞,kē|窠,kē|鈳,kē|榼,kē|薖,kē|颗,kē|樖,kē|瞌,kē|磕,kē|蝌,kē|頦,kē|醘,kē|顆,kē|髁,kē|礚,kē|嗑,kē|窼,kē|簻,kē|科,kē|壳,ké|咳,ké|揢,ké|翗,ké|嶱,ké|殼,ké|毼,kě|磆,kě|坷,kě|可,kě|岢,kě|炣,kě|渇,kě|嵑,kě|敤,kě|渴,kě|袔,kè|悈,kè|歁,kè|克,kè|刻,kè|剋,kè|勀,kè|勊,kè|客,kè|恪,kè|娔,kè|尅,kè|课,kè|堁,kè|氪,kè|骒,kè|缂,kè|愙,kè|溘,kè|锞,kè|碦,kè|課,kè|礊,kè|騍,kè|硞,kè|艐,kè|緙,kè|肎,kěn|肯,kěn|肻,kěn|垦,kěn|恳,kěn|啃,kěn|豤,kěn|貇,kěn|墾,kěn|錹,kěn|懇,kěn|頎,kěn|掯,kèn|裉,kèn|褃,kèn|硍,kèn|妔,kēng|踁,kēng|劥,kēng|吭,kēng|坈,kēng|坑,kēng|挳,kēng|硁,kēng|牼,kēng|硜,kēng|铿,kēng|硻,kēng|誙,kēng|銵,kēng|鏗,kēng|摼,kēng|殸,kēng|揁,kēng|鍞,kēng|巪,keo|乬,keol|唟,keos|厼,keum|怾,ki|空,kōng|倥,kōng|埪,kōng|崆,kōng|悾,kōng|硿,kōng|箜,kōng|躻,kōng|錓,kōng|鵼,kōng|椌,kōng|宆,kōng|孔,kǒng|恐,kǒng|控,kòng|鞚,kòng|羫,kòng|廤,kos|抠,kōu|芤,kōu|眍,kōu|剾,kōu|彄,kōu|摳,kōu|瞘,kōu|劶,kǒu|竘,kǒu|口,kǒu|叩,kòu|扣,kòu|怐,kòu|敂,kòu|冦,kòu|宼,kòu|寇,kòu|釦,kòu|窛,kòu|筘,kòu|滱,kòu|蔲,kòu|蔻,kòu|瞉,kòu|簆,kòu|鷇,kòu|搰,kū|刳,kū|矻,kū|郀,kū|枯,kū|哭,kū|桍,kū|堀,kū|崫,kū|圐,kū|跍,kū|窟,kū|骷,kū|泏,kū|窋,kū|狜,kǔ|苦,kǔ|楛,kǔ|齁,kù|捁,kù|库,kù|俈,kù|绔,kù|庫,kù|秙,kù|袴,kù|喾,kù|絝,kù|裤,kù|瘔,kù|酷,kù|褲,kù|嚳,kù|鮬,kù|恗,kuā|夸,kuā|姱,kuā|晇,kuā|舿,kuā|誇,kuā|侉,kuǎ|咵,kuǎ|垮,kuǎ|銙,kuǎ|顝,kuǎ|挎,kuà|胯,kuà|跨,kuà|骻,kuà|擓,kuai|蒯,kuǎi|璯,kuài|駃,kuài|巜,kuài|凷,kuài|圦,kuài|块,kuài|快,kuài|侩,kuài|郐,kuài|哙,kuài|狯,kuài|脍,kuài|塊,kuài|筷,kuài|鲙,kuài|儈,kuài|鄶,kuài|噲,kuài|廥,kuài|獪,kuài|膾,kuài|旝,kuài|糩,kuài|鱠,kuài|蕢,kuài|宽,kuān|寛,kuān|寬,kuān|髋,kuān|鑧,kuān|髖,kuān|欵,kuǎn|款,kuǎn|歀,kuǎn|窽,kuǎn|窾,kuǎn|梡,kuǎn|匡,kuāng|劻,kuāng|诓,kuāng|邼,kuāng|匩,kuāng|哐,kuāng|恇,kuāng|洭,kuāng|筐,kuāng|筺,kuāng|誆,kuāng|軭,kuāng|狂,kuáng|狅,kuáng|诳,kuáng|軖,kuáng|軠,kuáng|誑,kuáng|鵟,kuáng|夼,kuǎng|儣,kuǎng|懭,kuǎng|爌,kuǎng|邝,kuàng|圹,kuàng|况,kuàng|旷,kuàng|岲,kuàng|況,kuàng|矿,kuàng|昿,kuàng|贶,kuàng|框,kuàng|眖,kuàng|砿,kuàng|眶,kuàng|絋,kuàng|絖,kuàng|貺,kuàng|軦,kuàng|鉱,kuàng|鋛,kuàng|鄺,kuàng|壙,kuàng|黋,kuàng|懬,kuàng|曠,kuàng|礦,kuàng|穬,kuàng|纊,kuàng|鑛,kuàng|纩,kuàng|亏,kuī|刲,kuī|悝,kuī|盔,kuī|窥,kuī|聧,kuī|窺,kuī|虧,kuī|闚,kuī|巋,kuī|蘬,kuī|岿,kuī|奎,kuí|晆,kuí|逵,kuí|鄈,kuí|頄,kuí|馗,kuí|喹,kuí|揆,kuí|葵,kuí|骙,kuí|戣,kuí|暌,kuí|楏,kuí|楑,kuí|魁,kuí|睽,kuí|蝰,kuí|頯,kuí|櫆,kuí|藈,kuí|鍷,kuí|騤,kuí|夔,kuí|蘷,kuí|虁,kuí|躨,kuí|鍨,kuí|卼,kuǐ|煃,kuǐ|跬,kuǐ|頍,kuǐ|蹞,kuǐ|尯,kuǐ|匮,kuì|欳,kuì|喟,kuì|媿,kuì|愦,kuì|愧,kuì|溃,kuì|蒉,kuì|馈,kuì|匱,kuì|嘳,kuì|嬇,kuì|憒,kuì|潰,kuì|聩,kuì|聭,kuì|樻,kuì|殨,kuì|餽,kuì|簣,kuì|聵,kuì|籄,kuì|鐀,kuì|饋,kuì|鑎,kuì|篑,kuì|坤,kūn|昆,kūn|晜,kūn|堃,kūn|堒,kūn|婫,kūn|崐,kūn|崑,kūn|猑,kūn|菎,kūn|裈,kūn|焜,kūn|琨,kūn|髠,kūn|裩,kūn|锟,kūn|髡,kūn|尡,kūn|潉,kūn|蜫,kūn|褌,kūn|髨,kūn|熴,kūn|瑻,kūn|醌,kūn|錕,kūn|鲲,kūn|臗,kūn|騉,kūn|鯤,kūn|鵾,kūn|鶤,kūn|鹍,kūn|悃,kǔn|捆,kǔn|阃,kǔn|壸,kǔn|祵,kǔn|硱,kǔn|稇,kǔn|裍,kǔn|壼,kǔn|稛,kǔn|綑,kǔn|閫,kǔn|閸,kǔn|困,kùn|睏,kùn|涃,kùn|秳,kuò|漷,kuò|扩,kuò|拡,kuò|括,kuò|桰,kuò|筈,kuò|萿,kuò|葀,kuò|蛞,kuò|阔,kuò|廓,kuò|頢,kuò|擴,kuò|濶,kuò|闊,kuò|鞟,kuò|韕,kuò|懖,kuò|霩,kuò|鞹,kuò|鬠,kuò|穒,kweok|鞡,la|垃,lā|拉,lā|柆,lā|啦,lā|菈,lā|搚,lā|邋,lā|磖,lā|翋,lā|旯,lá|砬,lá|揦,lá|喇,lǎ|藞,lǎ|嚹,lǎ|剌,là|溂,là|腊,là|揧,là|楋,là|瘌,là|牎,chuāng|床,chuáng|漺,chuǎng|怆,chuàng|愴,chuàng|莊,zhuāng|粧,zhuāng|装,zhuāng|裝,zhuāng|樁,zhuāng|蜡,là|蝋,là|辢,là|辣,là|蝲,là|臈,là|攋,là|爉,là|臘,là|鬎,là|櫴,là|瓎,là|镴,là|鯻,là|鑞,là|儠,là|擸,là|鱲,là|蠟,là|来,lái|來,lái|俫,lái|倈,lái|崃,lái|徕,lái|涞,lái|莱,lái|郲,lái|婡,lái|崍,lái|庲,lái|徠,lái|梾,lái|淶,lái|猍,lái|萊,lái|逨,lái|棶,lái|琜,lái|筙,lái|铼,lái|箂,lái|錸,lái|騋,lái|鯠,lái|鶆,lái|麳,lái|顂,lái|勑,lài|誺,lài|赉,lài|睐,lài|睞,lài|赖,lài|賚,lài|濑,lài|賴,lài|頼,lài|癞,lài|鵣,lài|瀨,lài|瀬,lài|籁,lài|藾,lài|癩,lài|襰,lài|籟,lài|唻,lài|暕,lán|兰,lán|岚,lán|拦,lán|栏,lán|婪,lán|嵐,lán|葻,lán|阑,lán|蓝,lán|谰,lán|厱,lán|褴,lán|儖,lán|斓,lán|篮,lán|懢,lán|燣,lán|藍,lán|襕,lán|镧,lán|闌,lán|璼,lán|襤,lán|譋,lán|幱,lán|攔,lán|瀾,lán|灆,lán|籃,lán|繿,lán|蘭,lán|斕,lán|欄,lán|礷,lán|襴,lán|囒,lán|灡,lán|籣,lán|欗,lán|讕,lán|躝,lán|鑭,lán|钄,lán|韊,lán|惏,lán|澜,lán|襽,lán|览,lǎn|浨,lǎn|揽,lǎn|缆,lǎn|榄,lǎn|漤,lǎn|罱,lǎn|醂,lǎn|壈,lǎn|懒,lǎn|覧,lǎn|擥,lǎn|懶,lǎn|孄,lǎn|覽,lǎn|孏,lǎn|攬,lǎn|欖,lǎn|爦,lǎn|纜,lǎn|灠,lǎn|顲,lǎn|蘫,làn|嬾,làn|烂,làn|滥,làn|燗,làn|嚂,làn|壏,làn|濫,làn|爛,làn|爤,làn|瓓,làn|糷,làn|湅,làn|煉,làn|爁,làn|唥,lang|啷,lāng|羮,láng|勆,láng|郎,láng|郞,láng|欴,láng|狼,láng|嫏,láng|廊,láng|桹,láng|琅,láng|蓈,láng|榔,láng|瑯,láng|硠,láng|稂,láng|锒,láng|筤,láng|艆,láng|蜋,láng|郒,láng|螂,láng|躴,láng|鋃,láng|鎯,láng|阆,láng|閬,láng|哴,láng|悢,lǎng|朗,lǎng|朖,lǎng|烺,lǎng|塱,lǎng|蓢,lǎng|樃,lǎng|誏,lǎng|朤,lǎng|俍,lǎng|脼,lǎng|莨,làng|埌,làng|浪,làng|蒗,làng|捞,lāo|粩,lāo|撈,lāo|劳,láo|労,láo|牢,láo|窂,láo|哰,láo|崂,láo|浶,láo|勞,láo|痨,láo|僗,láo|嶗,láo|憥,láo|朥,láo|癆,láo|磱,láo|簩,láo|蟧,láo|醪,láo|鐒,láo|顟,láo|髝,láo|轑,láo|嫪,láo|憦,láo|铹,láo|耂,lǎo|老,lǎo|佬,lǎo|咾,lǎo|姥,lǎo|恅,lǎo|荖,lǎo|栳,lǎo|珯,lǎo|硓,lǎo|铑,lǎo|蛯,lǎo|銠,lǎo|橑,lǎo|鮱,lǎo|唠,lào|嘮,lào|烙,lào|嗠,lào|耢,lào|酪,lào|澇,lào|橯,lào|耮,lào|軂,lào|涝,lào|饹,le|了,le|餎,le|牞,lè|仂,lè|阞,lè|乐,lè|叻,lè|忇,lè|扐,lè|氻,lè|艻,lè|玏,lè|泐,lè|竻,lè|砳,lè|勒,lè|楽,lè|韷,lè|樂,lè|簕,lè|鳓,lè|鰳,lè|頛,lei|嘞,lei|雷,léi|嫘,léi|缧,léi|蔂,léi|樏,léi|畾,léi|檑,léi|縲,léi|镭,léi|櫑,léi|瓃,léi|羸,léi|礧,léi|罍,léi|蘲,léi|鐳,léi|轠,léi|壨,léi|鑘,léi|靁,léi|虆,léi|鱩,léi|欙,léi|纝,léi|鼺,léi|磥,léi|攂,léi|腂,lěi|瘣,lěi|厽,lěi|耒,lěi|诔,lěi|垒,lěi|絫,lěi|傫,lěi|誄,lěi|磊,lěi|蕌,lěi|蕾,lěi|儡,lěi|壘,lěi|癗,lěi|藟,lěi|櫐,lěi|矋,lěi|礨,lěi|灅,lěi|蠝,lěi|蘽,lěi|讄,lěi|儽,lěi|鑸,lěi|鸓,lěi|洡,lěi|礌,lěi|塁,lěi|纍,lèi|肋,lèi|泪,lèi|类,lèi|涙,lèi|淚,lèi|累,lèi|酹,lèi|銇,lèi|頪,lèi|擂,lèi|錑,lèi|颣,lèi|類,lèi|纇,lèi|蘱,lèi|禷,lèi|祱,lèi|塄,léng|棱,léng|楞,léng|碐,léng|稜,léng|踜,léng|薐,léng|輘,léng|冷,lěng|倰,lèng|堎,lèng|愣,lèng|睖,lèng|瓈,li|唎,lī|粚,lí|刕,lí|厘,lí|剓,lí|梨,lí|狸,lí|荲,lí|骊,lí|悡,lí|梸,lí|犁,lí|菞,lí|喱,lí|棃,lí|犂,lí|鹂,lí|剺,lí|漓,lí|睝,lí|筣,lí|缡,lí|艃,lí|蓠,lí|蜊,lí|嫠,lí|孷,lí|樆,lí|璃,lí|盠,lí|竰,lí|氂,lí|犛,lí|糎,lí|蔾,lí|鋫,lí|鲡,lí|黎,lí|篱,lí|縭,lí|罹,lí|錅,lí|蟍,lí|謧,lí|醨,lí|嚟,lí|藜,lí|邌,lí|釐,lí|離,lí|鯏,lí|鏫,lí|鯬,lí|鵹,lí|黧,lí|囄,lí|灕,lí|蘺,lí|蠡,lí|蠫,lí|孋,lí|廲,lí|劙,lí|鑗,lí|籬,lí|驪,lí|鱺,lí|鸝,lí|婯,lí|儷,lí|矖,lí|纚,lí|离,lí|褵,lí|穲,lí|礼,lǐ|李,lǐ|里,lǐ|俚,lǐ|峛,lǐ|哩,lǐ|娌,lǐ|峲,lǐ|浬,lǐ|逦,lǐ|理,lǐ|裡,lǐ|锂,lǐ|粴,lǐ|裏,lǐ|鋰,lǐ|鲤,lǐ|澧,lǐ|禮,lǐ|鯉,lǐ|蟸,lǐ|醴,lǐ|鳢,lǐ|邐,lǐ|鱧,lǐ|欐,lǐ|欚,lǐ|銐,lì|脷,lì|莉,lì|力,lì|历,lì|厉,lì|屴,lì|立,lì|吏,lì|朸,lì|丽,lì|利,lì|励,lì|呖,lì|坜,lì|沥,lì|苈,lì|例,lì|岦,lì|戾,lì|枥,lì|沴,lì|疠,lì|苙,lì|隶,lì|俐,lì|俪,lì|栃,lì|栎,lì|疬,lì|砅,lì|茘,lì|荔,lì|轹,lì|郦,lì|娳,lì|悧,lì|栗,lì|栛,lì|栵,lì|涖,lì|猁,lì|珕,lì|砺,lì|砾,lì|秝,lì|莅,lì|唳,lì|悷,lì|琍,lì|笠,lì|粒,lì|粝,lì|蚸,lì|蛎,lì|傈,lì|凓,lì|厤,lì|棙,lì|痢,lì|蛠,lì|詈,lì|雳,lì|塛,lì|慄,lì|搮,lì|溧,lì|蒚,lì|蒞,lì|鉝,lì|鳨,lì|厯,lì|厲,lì|暦,lì|歴,lì|瑮,lì|綟,lì|蜧,lì|勵,lì|曆,lì|歷,lì|篥,lì|隷,lì|鴗,lì|巁,lì|檪,lì|濿,lì|癘,lì|磿,lì|隸,lì|鬁,lì|儮,lì|櫔,lì|爄,lì|犡,lì|禲,lì|蠇,lì|嚦,lì|壢,lì|攊,lì|櫟,lì|瀝,lì|瓅,lì|礪,lì|藶,lì|麗,lì|櫪,lì|爏,lì|瓑,lì|皪,lì|盭,lì|礫,lì|糲,lì|蠣,lì|癧,lì|礰,lì|酈,lì|鷅,lì|麜,lì|囇,lì|攦,lì|轢,lì|讈,lì|轣,lì|攭,lì|瓥,lì|靂,lì|鱱,lì|靋,lì|觻,lì|鱳,lì|叓,lì|蝷,lì|赲,lì|曞,lì|嫾,liān|奁,lián|连,lián|帘,lián|怜,lián|涟,lián|莲,lián|連,lián|梿,lián|联,lián|裢,lián|亷,lián|嗹,lián|廉,lián|慩,lián|溓,lián|漣,lián|蓮,lián|奩,lián|熑,lián|覝,lián|劆,lián|匳,lián|噒,lián|憐,lián|磏,lián|聨,lián|聫,lián|褳,lián|鲢,lián|濂,lián|濓,lián|縺,lián|翴,lián|聮,lián|薕,lián|螊,lián|櫣,lián|燫,lián|聯,lián|臁,lián|蹥,lián|謰,lián|鎌,lián|镰,lián|簾,lián|蠊,lián|譧,lián|鐮,lián|鰱,lián|籢,lián|籨,lián|槤,lián|僆,lián|匲,lián|鬑,lián|敛,liǎn|琏,liǎn|脸,liǎn|裣,liǎn|摙,liǎn|璉,liǎn|蔹,liǎn|嬚,liǎn|斂,liǎn|歛,liǎn|臉,liǎn|鄻,liǎn|襝,liǎn|羷,liǎn|蘝,liǎn|蘞,liǎn|薟,liǎn|练,liàn|炼,liàn|恋,liàn|浰,liàn|殓,liàn|堜,liàn|媡,liàn|链,liàn|楝,liàn|瑓,liàn|潋,liàn|稴,liàn|練,liàn|澰,liàn|錬,liàn|殮,liàn|鍊,liàn|鏈,liàn|瀲,liàn|鰊,liàn|戀,liàn|纞,liàn|孌,liàn|攣,liàn|萰,liàn|簗,liāng|良,liáng|凉,liáng|梁,liáng|涼,liáng|椋,liáng|辌,liáng|粮,liáng|粱,liáng|墚,liáng|綡,liáng|輬,liáng|糧,liáng|駺,liáng|樑,liáng|冫,liǎng|俩,liǎng|倆,liǎng|両,liǎng|两,liǎng|兩,liǎng|唡,liǎng|啢,liǎng|掚,liǎng|裲,liǎng|緉,liǎng|蜽,liǎng|魉,liǎng|魎,liǎng|倞,liàng|靓,liàng|靚,liàng|踉,liàng|亮,liàng|谅,liàng|辆,liàng|喨,liàng|晾,liàng|湸,liàng|量,liàng|煷,liàng|輌,liàng|諒,liàng|輛,liàng|鍄,liàng|蹽,liāo|樛,liáo|潦,liáo|辽,liáo|疗,liáo|僚,liáo|寥,liáo|嵺,liáo|憀,liáo|漻,liáo|膋,liáo|嘹,liáo|嫽,liáo|寮,liáo|嶚,liáo|嶛,liáo|憭,liáo|撩,liáo|敹,liáo|獠,liáo|缭,liáo|遼,liáo|暸,liáo|燎,liáo|璙,liáo|窷,liáo|膫,liáo|療,liáo|竂,liáo|鹩,liáo|屪,liáo|廫,liáo|簝,liáo|蟟,liáo|豂,liáo|賿,liáo|蹘,liáo|爎,liáo|髎,liáo|飉,liáo|鷯,liáo|镽,liáo|尞,liáo|镠,liáo|鏐,liáo|僇,liáo|聊,liáo|繚,liáo|钌,liǎo|釕,liǎo|鄝,liǎo|蓼,liǎo|爒,liǎo|瞭,liǎo|廖,liào|镣,liào|鐐,liào|尥,liào|炓,liào|料,liào|撂,liào|蟉,liào|鴷,lie|咧,liě|毟,liě|挘,liě|埓,liě|忚,liě|列,liè|劣,liè|冽,liè|姴,liè|峢,liè|挒,liè|洌,liè|茢,liè|迾,liè|埒,liè|浖,liè|烈,liè|烮,liè|捩,liè|猎,liè|猟,liè|脟,liè|蛚,liè|裂,liè|煭,liè|睙,liè|聗,liè|趔,liè|巤,liè|颲,liè|鮤,liè|獵,liè|犣,liè|躐,liè|鬛,liè|哷,liè|劦,liè|奊,liè|劽,liè|鬣,liè|拎,līn|邻,lín|林,lín|临,lín|啉,lín|崊,lín|淋,lín|晽,lín|琳,lín|粦,lín|痳,lín|碄,lín|箖,lín|粼,lín|鄰,lín|隣,lín|嶙,lín|潾,lín|獜,lín|遴,lín|斴,lín|暽,lín|燐,lín|璘,lín|辚,lín|霖,lín|瞵,lín|磷,lín|繗,lín|翷,lín|麐,lín|轔,lín|壣,lín|瀶,lín|鏻,lín|鳞,lín|驎,lín|麟,lín|鱗,lín|疄,lín|蹸,lín|魿,lín|涁,lín|臨,lín|菻,lǐn|亃,lǐn|僯,lǐn|凛,lǐn|凜,lǐn|撛,lǐn|廩,lǐn|廪,lǐn|懍,lǐn|懔,lǐn|澟,lǐn|檁,lǐn|檩,lǐn|伈,lǐn|吝,lìn|恡,lìn|赁,lìn|焛,lìn|賃,lìn|蔺,lìn|橉,lìn|甐,lìn|膦,lìn|閵,lìn|藺,lìn|躏,lìn|躙,lìn|躪,lìn|轥,lìn|悋,lìn|伶,líng|刢,líng|灵,líng|囹,líng|坽,líng|夌,líng|姈,líng|岺,líng|彾,líng|泠,líng|狑,líng|苓,líng|昤,líng|柃,líng|玲,líng|瓴,líng|凌,líng|皊,líng|砱,líng|秢,líng|竛,líng|铃,líng|陵,líng|鸰,líng|婈,líng|崚,líng|掕,líng|棂,líng|淩,líng|琌,líng|笭,líng|紷,líng|绫,líng|羚,líng|翎,líng|聆,líng|舲,líng|菱,líng|蛉,líng|衑,líng|祾,líng|詅,líng|跉,líng|蓤,líng|裬,líng|鈴,líng|閝,líng|零,líng|龄,líng|綾,líng|蔆,líng|霊,líng|駖,líng|澪,líng|蕶,líng|錂,líng|霗,líng|鲮,líng|鴒,líng|鹷,líng|燯,líng|霛,líng|霝,líng|齢,líng|瀮,líng|酃,líng|鯪,líng|孁,líng|蘦,líng|齡,líng|櫺,líng|靈,líng|欞,líng|爧,líng|麢,líng|龗,líng|阾,líng|袊,líng|靇,líng|朎,líng|軨,líng|醽,líng|岭,lǐng|领,lǐng|領,lǐng|嶺,lǐng|令,lìng|另,lìng|呤,lìng|炩,lìng|溜,liū|熘,liū|澑,liū|蹓,liū|刘,liú|沠,liú|畄,liú|浏,liú|流,liú|留,liú|旈,liú|琉,liú|畱,liú|硫,liú|裗,liú|媹,liú|嵧,liú|旒,liú|蓅,liú|遛,liú|馏,liú|骝,liú|榴,liú|瑠,liú|飗,liú|劉,liú|瑬,liú|瘤,liú|磂,liú|镏,liú|駠,liú|鹠,liú|橊,liú|璢,liú|疁,liú|癅,liú|駵,liú|嚠,liú|懰,liú|瀏,liú|藰,liú|鎏,liú|鎦,liú|餾,liú|麍,liú|鐂,liú|騮,liú|飅,liú|鰡,liú|鶹,liú|驑,liú|蒥,liú|飀,liú|柳,liǔ|栁,liǔ|桞,liǔ|珋,liǔ|桺,liǔ|绺,liǔ|锍,liǔ|綹,liǔ|熮,liǔ|罶,liǔ|鋶,liǔ|橮,liǔ|羀,liǔ|嬼,liǔ|畂,liù|六,liù|翏,liù|塯,liù|廇,liù|磟,liù|鹨,liù|霤,liù|雡,liù|鬸,liù|鷚,liù|飂,liù|囖,lō|谾,lóng|龙,lóng|屸,lóng|咙,lóng|泷,lóng|茏,lóng|昽,lóng|栊,lóng|珑,lóng|胧,lóng|眬,lóng|砻,lóng|笼,lóng|聋,lóng|隆,lóng|湰,lóng|嶐,lóng|槞,lóng|漋,lóng|蕯,lóng|癃,lóng|窿,lóng|篭,lóng|龍,lóng|巃,lóng|巄,lóng|瀧,lóng|蘢,lóng|鏧,lóng|霳,lóng|曨,lóng|櫳,lóng|爖,lóng|瓏,lóng|矓,lóng|礱,lóng|礲,lóng|襱,lóng|籠,lóng|聾,lóng|蠪,lóng|蠬,lóng|龓,lóng|豅,lóng|躘,lóng|鑨,lóng|驡,lóng|鸗,lóng|滝,lóng|嚨,lóng|朧,lǒng|陇,lǒng|垄,lǒng|垅,lǒng|儱,lǒng|隴,lǒng|壟,lǒng|壠,lǒng|攏,lǒng|竉,lǒng|徿,lǒng|拢,lǒng|梇,lòng|衖,lòng|贚,lòng|喽,lou|嘍,lou|窶,lóu|娄,lóu|婁,lóu|溇,lóu|蒌,lóu|楼,lóu|廔,lóu|慺,lóu|蔞,lóu|遱,lóu|樓,lóu|熡,lóu|耧,lóu|蝼,lóu|艛,lóu|螻,lóu|謱,lóu|軁,lóu|髅,lóu|鞻,lóu|髏,lóu|漊,lóu|屚,lóu|膢,lóu|耬,lóu|嵝,lǒu|搂,lǒu|塿,lǒu|嶁,lǒu|摟,lǒu|甊,lǒu|篓,lǒu|簍,lǒu|陋,lòu|漏,lòu|瘘,lòu|镂,lòu|瘺,lòu|鏤,lòu|氌,lu|氇,lu|噜,lū|撸,lū|嚕,lū|擼,lū|卢,lú|芦,lú|垆,lú|枦,lú|泸,lú|炉,lú|栌,lú|胪,lú|轳,lú|舮,lú|鸬,lú|玈,lú|舻,lú|颅,lú|鈩,lú|鲈,lú|魲,lú|盧,lú|嚧,lú|壚,lú|廬,lú|攎,lú|瀘,lú|獹,lú|蘆,lú|櫨,lú|爐,lú|瓐,lú|臚,lú|矑,lú|纑,lú|罏,lú|艫,lú|蠦,lú|轤,lú|鑪,lú|顱,lú|髗,lú|鱸,lú|鸕,lú|黸,lú|鹵,lú|塷,lú|庐,lú|籚,lú|卤,lǔ|虏,lǔ|挔,lǔ|捛,lǔ|掳,lǔ|硵,lǔ|鲁,lǔ|虜,lǔ|滷,lǔ|蓾,lǔ|樐,lǔ|澛,lǔ|魯,lǔ|擄,lǔ|橹,lǔ|磠,lǔ|镥,lǔ|櫓,lǔ|艣,lǔ|鏀,lǔ|艪,lǔ|鐪,lǔ|鑥,lǔ|瀂,lǔ|露,lù|圥,lù|甪,lù|陆,lù|侓,lù|坴,lù|彔,lù|录,lù|峍,lù|勎,lù|赂,lù|辂,lù|陸,lù|娽,lù|淕,lù|淥,lù|渌,lù|硉,lù|菉,lù|逯,lù|鹿,lù|椂,lù|琭,lù|祿,lù|剹,lù|勠,lù|盝,lù|睩,lù|碌,lù|稑,lù|賂,lù|路,lù|輅,lù|塶,lù|廘,lù|摝,lù|漉,lù|箓,lù|粶,lù|蔍,lù|戮,lù|膟,lù|觮,lù|趢,lù|踛,lù|辘,lù|醁,lù|潞,lù|穋,lù|錄,lù|録,lù|錴,lù|璐,lù|簏,lù|螰,lù|鴼,lù|簶,lù|蹗,lù|轆,lù|騄,lù|鹭,lù|簬,lù|簵,lù|鯥,lù|鵦,lù|鵱,lù|麓,lù|鏴,lù|騼,lù|籙,lù|虂,lù|鷺,lù|緑,lù|攄,lù|禄,lù|蕗,lù|娈,luán|孪,luán|峦,luán|挛,luán|栾,luán|鸾,luán|脔,luán|滦,luán|銮,luán|鵉,luán|奱,luán|孿,luán|巒,luán|曫,luán|欒,luán|灓,luán|羉,luán|臠,luán|圞,luán|灤,luán|虊,luán|鑾,luán|癴,luán|癵,luán|鸞,luán|圝,luán|卵,luǎn|乱,luàn|釠,luàn|亂,luàn|乿,luàn|掠,luě|稤,luě|略,luè|畧,luè|锊,luè|圙,luè|鋝,luè|鋢,luè|剠,luè|擽,luè|抡,lún|掄,lún|仑,lún|伦,lún|囵,lún|沦,lún|纶,lún|轮,lún|倫,lún|陯,lún|圇,lún|婨,lún|崘,lún|崙,lún|惀,lún|淪,lún|菕,lún|棆,lún|腀,lún|碖,lún|綸,lún|蜦,lún|踚,lún|輪,lún|磮,lún|鯩,lún|耣,lún|稐,lǔn|埨,lǔn|侖,lùn|溣,lùn|論,lùn|论,lùn|頱,luō|囉,luō|啰,luō|罗,luó|猡,luó|脶,luó|萝,luó|逻,luó|椤,luó|腡,luó|锣,luó|箩,luó|骡,luó|镙,luó|螺,luó|羅,luó|覶,luó|鏍,luó|儸,luó|覼,luó|騾,luó|蘿,luó|邏,luó|欏,luó|鸁,luó|鑼,luó|饠,luó|驘,luó|攞,luó|籮,luó|剆,luǒ|倮,luǒ|砢,luǒ|蓏,luǒ|裸,luǒ|躶,luǒ|瘰,luǒ|蠃,luǒ|臝,luǒ|曪,luǒ|癳,luǒ|茖,luò|蛒,luò|硦,luò|泺,luò|峈,luò|洛,luò|络,luò|荦,luò|骆,luò|洜,luò|珞,luò|笿,luò|絡,luò|落,luò|摞,luò|漯,luò|犖,luò|雒,luò|鮥,luò|鵅,luò|濼,luò|纙,luò|挼,luò|跞,luò|駱,luò|瞜,lǘ|瘻,lǘ|驴,lǘ|闾,lǘ|榈,lǘ|馿,lǘ|氀,lǘ|櫚,lǘ|藘,lǘ|曥,lǘ|鷜,lǘ|驢,lǘ|閭,lǘ|偻,lǚ|僂,lǚ|吕,lǚ|呂,lǚ|侣,lǚ|郘,lǚ|侶,lǚ|旅,lǚ|梠,lǚ|焒,lǚ|祣,lǚ|稆,lǚ|铝,lǚ|屡,lǚ|絽,lǚ|缕,lǚ|屢,lǚ|膂,lǚ|膐,lǚ|褛,lǚ|鋁,lǚ|履,lǚ|褸,lǚ|儢,lǚ|縷,lǚ|穭,lǚ|捋,lǚ|穞,lǚ|寠,lǜ|滤,lǜ|濾,lǜ|寽,lǜ|垏,lǜ|律,lǜ|虑,lǜ|率,lǜ|绿,lǜ|嵂,lǜ|氯,lǜ|葎,lǜ|綠,lǜ|慮,lǜ|箻,lǜ|勴,lǜ|繂,lǜ|櫖,lǜ|爈,lǜ|鑢,lǜ|卛,lǜ|亇,ma|吗,ma|嗎,ma|嘛,ma|妈,mā|媽,mā|痲,mā|孖,mā|麻,má|嫲,má|蔴,má|犘,má|蟆,má|蟇,má|尛,má|马,mǎ|犸,mǎ|玛,mǎ|码,mǎ|蚂,mǎ|馬,mǎ|溤,mǎ|獁,mǎ|遤,mǎ|瑪,mǎ|碼,mǎ|螞,mǎ|鷌,mǎ|鰢,mǎ|傌,mǎ|榪,mǎ|鎷,mǎ|杩,mà|祃,mà|閁,mà|骂,mà|睰,mà|嘜,mà|禡,mà|罵,mà|駡,mà|礣,mà|鬕,mà|貍,mái|埋,mái|霾,mái|买,mǎi|荬,mǎi|買,mǎi|嘪,mǎi|蕒,mǎi|鷶,mǎi|唛,mài|劢,mài|佅,mài|売,mài|麦,mài|卖,mài|脈,mài|麥,mài|衇,mài|勱,mài|賣,mài|邁,mài|霡,mài|霢,mài|迈,mài|颟,mān|顢,mān|姏,mán|悗,mán|蛮,mán|慲,mán|摱,mán|馒,mán|槾,mán|樠,mán|瞒,mán|瞞,mán|鞔,mán|饅,mán|鳗,mán|鬗,mán|鬘,mán|蠻,mán|矕,mán|僈,mán|屘,mǎn|満,mǎn|睌,mǎn|满,mǎn|滿,mǎn|螨,mǎn|襔,mǎn|蟎,mǎn|鏋,mǎn|曼,màn|谩,màn|墁,màn|幔,màn|慢,màn|漫,màn|獌,màn|缦,màn|蔄,màn|蔓,màn|熳,màn|澷,màn|镘,màn|縵,màn|蟃,màn|鏝,màn|蘰,màn|鰻,màn|謾,màn|牤,māng|朚,máng|龒,máng|邙,máng|吂,máng|忙,máng|汒,máng|芒,máng|尨,máng|杗,máng|杧,máng|盲,máng|厖,máng|恾,máng|笀,máng|茫,máng|哤,máng|娏,máng|浝,máng|狵,máng|牻,máng|硭,máng|釯,máng|铓,máng|痝,máng|鋩,máng|駹,máng|蘉,máng|氓,máng|甿,máng|庬,máng|鹲,máng|鸏,máng|莽,mǎng|茻,mǎng|壾,mǎng|漭,mǎng|蟒,mǎng|蠎,mǎng|莾,mǎng|匁,mangmi|猫,māo|貓,māo|毛,máo|矛,máo|枆,máo|牦,máo|茅,máo|旄,máo|渵,máo|軞,máo|酕,máo|堥,máo|锚,máo|緢,máo|髦,máo|髳,máo|錨,máo|蟊,máo|鶜,máo|茆,máo|罞,máo|鉾,máo|冇,mǎo|戼,mǎo|峁,mǎo|泖,mǎo|昴,mǎo|铆,mǎo|笷,mǎo|蓩,mǎo|鉚,mǎo|卯,mǎo|秏,mào|冃,mào|皃,mào|芼,mào|冐,mào|茂,mào|冒,mào|贸,mào|耄,mào|袤,mào|覒,mào|媢,mào|帽,mào|貿,mào|鄚,mào|愗,mào|暓,mào|楙,mào|毷,mào|瑁,mào|貌,mào|鄮,mào|蝐,mào|懋,mào|霿,mào|獏,mào|毣,mào|萺,mào|瞀,mào|唜,mas|么,me|嚜,me|麼,me|麽,me|庅,mē|嚒,mē|孭,mē|濹,mè|嚰,mè|沒,méi|没,méi|枚,méi|玫,méi|苺,méi|栂,méi|眉,méi|脄,méi|莓,méi|梅,méi|珻,méi|脢,méi|郿,méi|堳,méi|媒,méi|嵋,méi|湄,méi|湈,méi|睂,méi|葿,méi|楣,méi|楳,méi|煤,méi|瑂,méi|禖,méi|腜,méi|塺,méi|槑,méi|酶,méi|镅,méi|鹛,méi|鋂,méi|霉,méi|徾,méi|鎇,méi|矀,méi|攗,méi|蘪,méi|鶥,méi|攟,méi|黴,méi|坆,méi|猸,méi|羙,měi|毎,měi|每,měi|凂,měi|美,měi|挴,měi|浼,měi|媄,měi|渼,měi|媺,měi|镁,měi|嬍,měi|燘,měi|躾,měi|鎂,měi|黣,měi|嵄,měi|眊,mèi|妹,mèi|抺,mèi|沬,mèi|昧,mèi|祙,mèi|袂,mèi|眛,mèi|媚,mèi|寐,mèi|痗,mèi|跊,mèi|鬽,mèi|煝,mèi|睸,mèi|魅,mèi|篃,mèi|蝞,mèi|櫗,mèi|氼,mèi|们,men|們,men|椚,mēn|门,mén|扪,mén|钔,mén|門,mén|閅,mén|捫,mén|菛,mén|璊,mén|穈,mén|鍆,mén|虋,mén|怋,mén|玣,mén|殙,mèn|闷,mèn|焖,mèn|悶,mèn|暪,mèn|燜,mèn|懑,mèn|懣,mèn|掹,mēng|擝,mēng|懞,mēng|虻,méng|冡,méng|莔,méng|萌,méng|萠,méng|盟,méng|甍,méng|儚,méng|橗,méng|瞢,méng|蕄,méng|蝱,méng|鄳,méng|鄸,méng|幪,méng|濛,méng|獴,méng|曚,méng|朦,méng|檬,méng|氋,méng|礞,méng|鯍,méng|艨,méng|矒,méng|靀,méng|饛,méng|顭,méng|蒙,méng|鼆,méng|夣,méng|懜,méng|溕,méng|矇,měng|勐,měng|猛,měng|锰,měng|艋,měng|蜢,měng|錳,měng|懵,měng|蠓,měng|鯭,měng|黽,měng|瓾,měng|夢,mèng|孟,mèng|梦,mèng|霥,mèng|踎,meo|咪,mī|瞇,mī|眯,mī|冞,mí|弥,mí|祢,mí|迷,mí|袮,mí|猕,mí|谜,mí|蒾,mí|詸,mí|謎,mí|醚,mí|彌,mí|糜,mí|縻,mí|麊,mí|麋,mí|禰,mí|靡,mí|獼,mí|麛,mí|爢,mí|瓕,mí|蘼,mí|镾,mí|醾,mí|醿,mí|鸍,mí|釄,mí|檷,mí|籋,mí|罙,mí|擟,mí|米,mǐ|羋,mǐ|芈,mǐ|侎,mǐ|沵,mǐ|弭,mǐ|洣,mǐ|敉,mǐ|粎,mǐ|脒,mǐ|葞,mǐ|蝆,mǐ|蔝,mǐ|銤,mǐ|瀰,mǐ|孊,mǐ|灖,mǐ|渳,mǐ|哋,mì|汨,mì|沕,mì|宓,mì|泌,mì|觅,mì|峚,mì|宻,mì|秘,mì|密,mì|淧,mì|覓,mì|覔,mì|幂,mì|谧,mì|塓,mì|幎,mì|覛,mì|嘧,mì|榓,mì|漞,mì|熐,mì|蔤,mì|蜜,mì|鼏,mì|冪,mì|樒,mì|幦,mì|濗,mì|藌,mì|謐,mì|櫁,mì|簚,mì|羃,mì|鑖,mì|蓂,mì|滵,mì|芇,mián|眠,mián|婂,mián|绵,mián|媔,mián|棉,mián|綿,mián|緜,mián|蝒,mián|嬵,mián|檰,mián|櫋,mián|矈,mián|矊,mián|蠠,mián|矏,mián|厸,miǎn|丏,miǎn|汅,miǎn|免,miǎn|沔,miǎn|黾,miǎn|俛,miǎn|勉,miǎn|眄,miǎn|娩,miǎn|偭,miǎn|冕,miǎn|勔,miǎn|喕,miǎn|愐,miǎn|湎,miǎn|缅,miǎn|葂,miǎn|腼,miǎn|緬,miǎn|鮸,miǎn|渑,miǎn|澠,miǎn|靦,miǎn|靣,miàn|面,miàn|糆,miàn|麪,miàn|麫,miàn|麺,miàn|麵,miàn|喵,miāo|苗,miáo|媌,miáo|瞄,miáo|鹋,miáo|嫹,miáo|鶓,miáo|鱙,miáo|描,miáo|訬,miǎo|仯,miǎo|杪,miǎo|眇,miǎo|秒,miǎo|淼,miǎo|渺,miǎo|缈,miǎo|篎,miǎo|緲,miǎo|藐,miǎo|邈,miǎo|妙,miào|庙,miào|竗,miào|庿,miào|廟,miào|吀,miē|咩,miē|哶,miē|灭,miè|搣,miè|滅,miè|薎,miè|幭,miè|懱,miè|篾,miè|蠛,miè|衊,miè|鱴,miè|蔑,miè|民,mín|垊,mín|姄,mín|岷,mín|旻,mín|旼,mín|玟,mín|苠,mín|珉,mín|盿,mín|冧,mín|罠,mín|崏,mín|捪,mín|琘,mín|琝,mín|暋,mín|瑉,mín|痻,mín|碈,mín|鈱,mín|賯,mín|錉,mín|鍲,mín|缗,mín|湏,mǐn|緍,mǐn|緡,mǐn|皿,mǐn|冺,mǐn|刡,mǐn|闵,mǐn|抿,mǐn|泯,mǐn|勄,mǐn|敃,mǐn|闽,mǐn|悯,mǐn|敏,mǐn|笢,mǐn|笽,mǐn|湣,mǐn|閔,mǐn|愍,mǐn|敯,mǐn|閩,mǐn|慜,mǐn|憫,mǐn|潣,mǐn|簢,mǐn|鳘,mǐn|鰵,mǐn|僶,mǐn|名,míng|明,míng|鸣,míng|洺,míng|眀,míng|茗,míng|冥,míng|朙,míng|眳,míng|铭,míng|鄍,míng|嫇,míng|溟,míng|猽,míng|暝,míng|榠,míng|銘,míng|鳴,míng|瞑,míng|螟,míng|覭,míng|佲,mǐng|凕,mǐng|慏,mǐng|酩,mǐng|姳,mǐng|命,mìng|掵,mìng|詺,mìng|谬,miù|缪,miù|繆,miù|謬,miù|摸,mō|嚤,mō|嬤,mó|嬷,mó|戂,mó|攠,mó|谟,mó|嫫,mó|馍,mó|摹,mó|模,mó|膜,mó|摩,mó|魹,mó|橅,mó|磨,mó|糢,mó|謨,mó|謩,mó|擵,mó|饃,mó|蘑,mó|髍,mó|魔,mó|劘,mó|饝,mó|嚩,mó|懡,mǒ|麿,mǒ|狢,mò|貈,mò|貉,mò|脉,mò|瀎,mò|抹,mò|末,mò|劰,mò|圽,mò|妺,mò|怽,mò|歿,mò|殁,mò|沫,mò|茉,mò|陌,mò|帞,mò|昩,mò|枺,mò|皌,mò|眜,mò|眿,mò|砞,mò|秣,mò|莈,mò|眽,mò|粖,mò|絈,mò|蛨,mò|貃,mò|嗼,mò|塻,mò|寞,mò|漠,mò|蓦,mò|貊,mò|銆,mò|墨,mò|嫼,mò|暯,mò|瘼,mò|瞐,mò|瞙,mò|镆,mò|魩,mò|黙,mò|縸,mò|默,mò|貘,mò|藦,mò|蟔,mò|鏌,mò|爅,mò|礳,mò|纆,mò|耱,mò|艒,mò|莫,mò|驀,mò|乮,mol|哞,mōu|呣,móu|蛑,móu|蝥,móu|牟,móu|侔,móu|劺,móu|恈,móu|洠,móu|眸,móu|谋,móu|謀,móu|鍪,móu|鴾,móu|麰,móu|鞪,móu|某,mǒu|呒,mú|嘸,mú|毪,mú|氁,mú|母,mǔ|亩,mǔ|牡,mǔ|姆,mǔ|拇,mǔ|牳,mǔ|畆,mǔ|畒,mǔ|胟,mǔ|畝,mǔ|畞,mǔ|砪,mǔ|畮,mǔ|鉧,mǔ|踇,mǔ|坶,mǔ|峔,mǔ|朷,mù|木,mù|仫,mù|目,mù|凩,mù|沐,mù|狇,mù|炑,mù|牧,mù|苜,mù|莯,mù|蚞,mù|钼,mù|募,mù|雮,mù|墓,mù|幕,mù|慔,mù|楘,mù|睦,mù|鉬,mù|慕,mù|暮,mù|樢,mù|霂,mù|穆,mù|幙,mù|旀,myeo|椧,myeong|秅,ná|拏,ná|拿,ná|挐,ná|誽,ná|镎,ná|鎿,ná|乸,ná|詉,ná|蒘,ná|訤,ná|哪,nǎ|雫,nǎ|郍,nǎ|那,nà|吶,nà|妠,nà|纳,nà|肭,nà|娜,nà|钠,nà|納,nà|袦,nà|捺,nà|笝,nà|豽,nà|軜,nà|鈉,nà|嗱,nà|蒳,nà|靹,nà|魶,nà|呐,nà|內,nà|篛,nà|衲,nà|腉,nái|熋,nái|摨,nái|孻,nái|螚,nái|搱,nái|乃,nǎi|奶,nǎi|艿,nǎi|氖,nǎi|疓,nǎi|妳,nǎi|廼,nǎi|迺,nǎi|倷,nǎi|釢,nǎi|奈,nài|柰,nài|萘,nài|渿,nài|鼐,nài|褦,nài|錼,nài|耐,nài|囡,nān|男,nán|抩,nán|枏,nán|枬,nán|侽,nán|南,nán|柟,nán|娚,nán|畘,nán|莮,nán|难,nán|喃,nán|遖,nán|暔,nán|楠,nán|煵,nán|諵,nán|難,nán|萳,nán|嫨,nǎn|赧,nǎn|揇,nǎn|湳,nǎn|腩,nǎn|戁,nǎn|蝻,nǎn|婻,nàn|囔,nāng|涳,náng|乪,náng|嚢,náng|囊,náng|蠰,náng|鬞,náng|馕,náng|欜,náng|饢,náng|搑,náng|崀,nǎng|擃,nǎng|曩,nǎng|攮,nǎng|灢,nǎng|瀼,nǎng|儾,nàng|齉,nàng|孬,nāo|檂,nāo|巙,náo|呶,náo|怓,náo|挠,náo|峱,náo|硇,náo|铙,náo|猱,náo|蛲,náo|碙,náo|撓,náo|獶,náo|蟯,náo|夒,náo|譊,náo|鐃,náo|巎,náo|獿,náo|憹,náo|蝚,náo|嶩,náo|垴,nǎo|恼,nǎo|悩,nǎo|脑,nǎo|匘,nǎo|脳,nǎo|堖,nǎo|惱,nǎo|嫐,nǎo|瑙,nǎo|腦,nǎo|碯,nǎo|闹,nào|婥,nào|淖,nào|閙,nào|鬧,nào|臑,nào|呢,ne|讷,nè|抐,nè|眲,nè|訥,nè|娞,něi|馁,něi|腇,něi|餒,něi|鮾,něi|鯘,něi|浽,něi|内,nèi|氝,nèi|焾,nem|嫩,nèn|媆,nèn|嫰,nèn|竜,néng|能,néng|莻,neus|鈪,ngag|銰,ngai|啱,ngam|妮,nī|尼,ní|坭,ní|怩,ní|泥,ní|籾,ní|倪,ní|屔,ní|秜,ní|郳,ní|铌,ní|埿,ní|婗,ní|猊,ní|蚭,ní|棿,ní|跜,ní|鈮,ní|蜺,ní|觬,ní|貎,ní|霓,ní|鲵,ní|鯢,ní|麑,ní|齯,ní|臡,ní|抳,ní|蛪,ní|腝,ní|淣,ní|聻,nǐ|濔,nǐ|伱,nǐ|你,nǐ|拟,nǐ|狔,nǐ|苨,nǐ|柅,nǐ|旎,nǐ|晲,nǐ|孴,nǐ|鉨,nǐ|馜,nǐ|隬,nǐ|擬,nǐ|薿,nǐ|鑈,nǐ|儞,nǐ|伲,nì|迡,nì|昵,nì|胒,nì|逆,nì|匿,nì|痆,nì|眤,nì|堄,nì|惄,nì|嫟,nì|愵,nì|溺,nì|睨,nì|腻,nì|暱,nì|縌,nì|膩,nì|嬺,nì|灄,nì|孨,nì|拈,niān|蔫,niān|年,nián|秊,nián|哖,nián|秥,nián|鮎,nián|鲶,nián|鵇,nián|黏,nián|鯰,nián|姩,nián|鲇,nián|跈,niǎn|涊,niǎn|捻,niǎn|淰,niǎn|辇,niǎn|撚,niǎn|撵,niǎn|碾,niǎn|輦,niǎn|簐,niǎn|攆,niǎn|蹨,niǎn|躎,niǎn|辗,niǎn|輾,niǎn|卄,niàn|廿,niàn|念,niàn|埝,niàn|艌,niàn|娘,niáng|嬢,niáng|醸,niáng|酿,niàng|釀,niàng|茮,niǎo|尦,niǎo|鸟,niǎo|袅,niǎo|鳥,niǎo|嫋,niǎo|裊,niǎo|蔦,niǎo|嬝,niǎo|褭,niǎo|嬲,niǎo|茑,niǎo|尿,niào|脲,niào|捏,niē|揑,niē|乜,niè|帇,niè|圼,niè|苶,niè|枿,niè|陧,niè|涅,niè|聂,niè|臬,niè|啮,niè|惗,niè|菍,niè|隉,niè|喦,niè|敜,niè|嗫,niè|嵲,niè|踂,niè|摰,niè|槷,niè|踗,niè|踙,niè|镊,niè|镍,niè|嶭,niè|篞,niè|臲,niè|錜,niè|颞,niè|蹑,niè|嚙,niè|聶,niè|鎳,niè|闑,niè|孼,niè|孽,niè|櫱,niè|蘖,niè|囁,niè|齧,niè|巕,niè|糱,niè|糵,niè|蠥,niè|囓,niè|躡,niè|鑷,niè|顳,niè|諗,niè|囐,niè|銸,niè|鋷,niè|讘,niè|脌,nīn|囜,nín|您,nín|恁,nín|拰,nǐn|宁,níng|咛,níng|狞,níng|柠,níng|聍,níng|寍,níng|寕,níng|寜,níng|寧,níng|儜,níng|凝,níng|嚀,níng|嬣,níng|獰,níng|薴,níng|檸,níng|聹,níng|鑏,níng|鬡,níng|鸋,níng|甯,níng|濘,níng|鬤,níng|拧,nǐng|擰,nǐng|矃,nǐng|橣,nǐng|佞,nìng|侫,nìng|泞,nìng|寗,nìng|澝,nìng|妞,niū|牛,niú|牜,niú|忸,niǔ|扭,niǔ|沑,niǔ|狃,niǔ|纽,niǔ|杻,niǔ|炄,niǔ|钮,niǔ|紐,niǔ|莥,niǔ|鈕,niǔ|靵,niǔ|拗,niù|莀,nóng|农,nóng|侬,nóng|哝,nóng|浓,nóng|脓,nóng|秾,nóng|儂,nóng|辳,nóng|噥,nóng|濃,nóng|蕽,nóng|禯,nóng|膿,nóng|穠,nóng|襛,nóng|醲,nóng|欁,nóng|癑,nóng|農,nóng|繷,nǒng|廾,nòng|弄,nòng|挊,nòng|挵,nòng|齈,nòng|羺,nóu|譨,nóu|啂,nǒu|槈,nòu|耨,nòu|獳,nòu|檽,nòu|鎒,nòu|鐞,nòu|譳,nòu|嬬,nòu|奴,nú|驽,nú|笯,nú|駑,nú|砮,nú|孥,nú|伮,nǔ|努,nǔ|弩,nǔ|胬,nǔ|怒,nù|傉,nù|搙,nù|奻,nuán|渜,nuán|暖,nuǎn|煗,nuǎn|餪,nuǎn|疟,nuè|虐,nuè|瘧,nuè|硸,nuè|黁,nun|燶,nung|挪,nuó|梛,nuó|傩,nuó|搻,nuó|儺,nuó|橠,nuó|袲,nuǒ|诺,nuò|喏,nuò|掿,nuò|逽,nuò|搦,nuò|锘,nuò|榒,nuò|稬,nuò|諾,nuò|蹃,nuò|糑,nuò|懦,nuò|懧,nuò|糥,nuò|穤,nuò|糯,nuò|堧,nuò|耎,nuò|愞,nuò|女,nǚ|钕,nǚ|籹,nǚ|釹,nǚ|衂,nǜ|恧,nǜ|朒,nǜ|衄,nǜ|筽,o|噢,ō|哦,ò|夞,oes|乯,ol|鞰,on|吽,ōu|讴,ōu|欧,ōu|殴,ōu|瓯,ōu|鸥,ōu|塸,ōu|歐,ōu|毆,ōu|熰,ōu|甌,ōu|膒,ōu|鴎,ōu|櫙,ōu|藲,ōu|謳,ōu|鏂,ōu|鷗,ōu|沤,ōu|蓲,ōu|敺,ōu|醧,ōu|漚,ōu|齵,óu|澫,ǒu|吘,ǒu|呕,ǒu|偶,ǒu|腢,ǒu|嘔,ǒu|耦,ǒu|蕅,ǒu|藕,ǒu|怄,òu|慪,òu|妑,pā|趴,pā|舥,pā|啪,pā|葩,pā|帊,pā|杷,pá|爬,pá|耙,pá|掱,pá|琶,pá|筢,pá|潖,pá|跁,pá|帕,pà|怕,pà|袙,pà|拍,pāi|俳,pái|徘,pái|排,pái|猅,pái|牌,pái|輫,pái|簰,pái|犤,pái|哌,pài|派,pài|蒎,pài|鎃,pài|湃,pài|磗,pak|眅,pān|畨,pān|潘,pān|攀,pān|膰,pán|爿,pán|柈,pán|盘,pán|媻,pán|幋,pán|蒰,pán|槃,pán|盤,pán|磐,pán|縏,pán|蹒,pán|瀊,pán|蟠,pán|蹣,pán|鎜,pán|鞶,pán|踫,pán|宷,pán|洀,pán|闆,pǎn|坢,pǎn|盻,pǎn|眫,pàn|冸,pàn|判,pàn|沜,pàn|泮,pàn|叛,pàn|牉,pàn|盼,pàn|畔,pàn|袢,pàn|詊,pàn|溿,pàn|頖,pàn|鋬,pàn|鵥,pàn|襻,pàn|鑻,pàn|炍,pàn|乓,pāng|汸,pāng|沗,pāng|肨,pāng|胮,pāng|雱,pāng|滂,pāng|膖,pāng|霶,pāng|磅,páng|趽,páng|彷,páng|夆,páng|厐,páng|庞,páng|逄,páng|旁,páng|舽,páng|篣,páng|螃,páng|鳑,páng|龐,páng|鰟,páng|蠭,páng|髈,páng|龎,páng|耪,pǎng|覫,pǎng|炐,pàng|胖,pàng|抛,pāo|拋,pāo|脬,pāo|刨,páo|咆,páo|垉,páo|庖,páo|狍,páo|炰,páo|爮,páo|袍,páo|匏,páo|軳,páo|鞄,páo|褜,páo|麅,páo|颮,páo|跑,pǎo|窌,pào|炮,pào|奅,pào|泡,pào|皰,pào|砲,pào|萢,pào|麭,pào|礟,pào|礮,pào|犥,pào|疱,pào|妚,pēi|呸,pēi|怌,pēi|肧,pēi|胚,pēi|衃,pēi|醅,pēi|抷,pēi|阫,péi|陪,péi|陫,péi|培,péi|毰,péi|赔,péi|锫,péi|裴,péi|裵,péi|賠,péi|錇,péi|駍,péi|婄,péi|俖,pěi|茷,pèi|攈,pèi|伂,pèi|沛,pèi|佩,pèi|帔,pèi|姵,pèi|旆,pèi|浿,pèi|珮,pèi|配,pèi|笩,pèi|蓜,pèi|辔,pèi|馷,pèi|嶏,pèi|霈,pèi|轡,pèi|斾,pèi|喷,pēn|噴,pēn|濆,pēn|歕,pēn|衯,pén|瓫,pén|盆,pén|湓,pén|葐,pén|呠,pěn|翸,pěn|匉,pēng|怦,pēng|抨,pēng|泙,pēng|恲,pēng|胓,pēng|砰,pēng|烹,pēng|硑,pēng|軯,pēng|閛,pēng|漰,pēng|嘭,pēng|磞,pēng|弸,pēng|荓,pēng|軿,pēng|輧,pēng|梈,pēng|芃,péng|朋,péng|竼,péng|倗,péng|莑,péng|堋,péng|彭,péng|棚,péng|椖,péng|塜,péng|塳,péng|漨,péng|硼,péng|稝,péng|蓬,péng|鹏,péng|槰,péng|樥,péng|憉,péng|澎,péng|輣,péng|篷,péng|膨,péng|韸,péng|髼,péng|蟚,péng|蟛,péng|鬅,péng|纄,péng|韼,péng|鵬,péng|鬔,péng|鑝,péng|淜,péng|熢,péng|摓,pěng|捧,pěng|淎,pěng|皏,pěng|剻,pěng|掽,pèng|椪,pèng|碰,pèng|浌,peol|巼,phas|闏,phdeng|乶,phoi|喸,phos|榌,pi|伓,pī|伾,pī|批,pī|纰,pī|邳,pī|坯,pī|披,pī|炋,pī|狉,pī|狓,pī|砒,pī|秛,pī|秠,pī|紕,pī|耚,pī|豾,pī|釽,pī|鉟,pī|銔,pī|劈,pī|磇,pī|駓,pī|噼,pī|錃,pī|魾,pī|憵,pī|礔,pī|礕,pī|霹,pī|鲏,pī|鮍,pī|丕,pī|髬,pī|铍,pí|鈹,pí|皮,pí|阰,pí|芘,pí|岯,pí|枇,pí|毞,pí|毗,pí|毘,pí|疲,pí|蚍,pí|郫,pí|陴,pí|啤,pí|埤,pí|蚽,pí|豼,pí|焷,pí|脾,pí|腗,pí|罴,pí|膍,pí|蜱,pí|隦,pí|壀,pí|篺,pí|螷,pí|貔,pí|簲,pí|羆,pí|鵧,pí|朇,pí|鼙,pí|蠯,pí|猈,pí|琵,pí|匹,pǐ|庀,pǐ|仳,pǐ|圮,pǐ|苉,pǐ|脴,pǐ|痞,pǐ|銢,pǐ|鴄,pǐ|噽,pǐ|癖,pǐ|嚭,pǐ|顖,pǐ|擗,pǐ|辟,pì|鈲,pì|闢,pì|屁,pì|淠,pì|渒,pì|揊,pì|媲,pì|嫓,pì|睤,pì|睥,pì|潎,pì|僻,pì|澼,pì|嚊,pì|甓,pì|疈,pì|譬,pì|鷿,pì|囨,piān|偏,piān|媥,piān|犏,piān|篇,piān|翩,piān|骈,pián|胼,pián|楄,pián|楩,pián|賆,pián|諚,pián|骿,pián|蹁,pián|駢,pián|騈,pián|徧,pián|腁,pián|覑,piǎn|谝,piǎn|貵,piǎn|諞,piǎn|片,piàn|骗,piàn|魸,piàn|騗,piàn|騙,piàn|剽,piāo|彯,piāo|漂,piāo|缥,piāo|飘,piāo|磦,piāo|旚,piāo|縹,piāo|翲,piāo|螵,piāo|飄,piāo|魒,piāo|薸,piáo|闝,piáo|嫖,piáo|瓢,piáo|莩,piǎo|殍,piǎo|瞟,piǎo|醥,piǎo|皫,piǎo|顠,piǎo|飃,piào|票,piào|勡,piào|嘌,piào|慓,piào|覕,piē|氕,piē|撆,piē|暼,piē|瞥,piē|撇,piě|丿,piě|苤,piě|鐅,piě|嫳,piè|拚,pīn|姘,pīn|拼,pīn|礗,pīn|穦,pīn|馪,pīn|驞,pīn|贫,pín|貧,pín|嫔,pín|频,pín|頻,pín|嬪,pín|薲,pín|嚬,pín|矉,pín|颦,pín|顰,pín|蘋,pín|玭,pín|品,pǐn|榀,pǐn|朩,pìn|牝,pìn|汖,pìn|聘,pìn|娉,pīng|乒,pīng|甹,pīng|俜,pīng|涄,pīng|砯,pīng|艵,pīng|竮,pīng|頩,pīng|冖,píng|平,píng|评,píng|凭,píng|坪,píng|岼,píng|苹,píng|郱,píng|屏,píng|帡,píng|枰,píng|洴,píng|玶,píng|娦,píng|瓶,píng|屛,píng|帲,píng|萍,píng|蚲,píng|塀,píng|幈,píng|焩,píng|甁,píng|缾,píng|聠,píng|蓱,píng|蛢,píng|評,píng|鲆,píng|凴,píng|慿,píng|憑,píng|鮃,píng|簈,píng|呯,píng|箳,píng|鏺,po|钋,pō|坡,pō|岥,pō|泼,pō|釙,pō|颇,pō|溌,pō|酦,pō|潑,pō|醱,pō|頗,pō|攴,pō|巿,pó|婆,pó|嘙,pó|鄱,pó|皤,pó|謈,pó|櫇,pó|叵,pǒ|尀,pǒ|钷,pǒ|笸,pǒ|鉕,pǒ|駊,pǒ|屰,pò|廹,pò|岶,pò|迫,pò|敀,pò|昢,pò|洦,pò|珀,pò|烞,pò|破,pò|砶,pò|粕,pò|奤,pò|蒪,pò|魄,pò|皛,pò|頮,pōu|剖,pōu|颒,pōu|抙,pōu|捊,pōu|抔,póu|掊,póu|裒,póu|咅,pǒu|哣,pǒu|犃,pǒu|兺,ppun|哛,ppun|巬,pu|巭,pu|扑,pū|炇,pū|痡,pū|駇,pū|噗,pū|撲,pū|鋪,pū|潽,pū|襆,pú|脯,pú|蜅,pú|仆,pú|圤,pú|匍,pú|莆,pú|菩,pú|菐,pú|葡,pú|蒱,pú|蒲,pú|僕,pú|酺,pú|墣,pú|璞,pú|瞨,pú|穙,pú|镤,pú|贌,pú|纀,pú|鏷,pú|襥,pú|濮,pú|朴,pǔ|圃,pǔ|埔,pǔ|浦,pǔ|烳,pǔ|普,pǔ|圑,pǔ|溥,pǔ|暜,pǔ|谱,pǔ|樸,pǔ|氆,pǔ|諩,pǔ|檏,pǔ|镨,pǔ|譜,pǔ|蹼,pǔ|鐠,pǔ|铺,pù|舖,pù|舗,pù|曝,pù|七,qī|沏,qī|妻,qī|恓,qī|柒,qī|倛,qī|凄,qī|栖,qī|桤,qī|缼,qī|郪,qī|娸,qī|戚,qī|捿,qī|桼,qī|淒,qī|萋,qī|朞,qī|期,qī|棲,qī|欺,qī|紪,qī|褄,qī|僛,qī|嘁,qī|慽,qī|榿,qī|漆,qī|緀,qī|磎,qī|諆,qī|諿,qī|霋,qī|蹊,qī|魌,qī|鏚,qī|鶈,qī|碕,qī|螇,qī|傶,qī|迉,qī|軙,qí|荎,qí|饑,qí|亓,qí|祁,qí|齐,qí|圻,qí|岐,qí|岓,qí|忯,qí|芪,qí|亝,qí|其,qí|奇,qí|斉,qí|歧,qí|祇,qí|祈,qí|肵,qí|疧,qí|竒,qí|剘,qí|斊,qí|旂,qí|脐,qí|蚑,qí|蚔,qí|蚚,qí|颀,qí|埼,qí|崎,qí|掑,qí|淇,qí|渏,qí|猉,qí|畦,qí|萁,qí|跂,qí|軝,qí|釮,qí|骐,qí|骑,qí|嵜,qí|棊,qí|棋,qí|琦,qí|琪,qí|祺,qí|蛴,qí|愭,qí|碁,qí|鬿,qí|旗,qí|粸,qí|綥,qí|綦,qí|綨,qí|緕,qí|蜝,qí|蜞,qí|齊,qí|禥,qí|蕲,qí|螧,qí|鲯,qí|濝,qí|藄,qí|檱,qí|櫀,qí|簱,qí|臍,qí|騎,qí|騏,qí|鳍,qí|蘄,qí|鵸,qí|鶀,qí|麒,qí|籏,qí|纃,qí|艩,qí|蠐,qí|鬐,qí|騹,qí|魕,qí|鰭,qí|玂,qí|麡,qí|荠,qí|薺,qí|扺,qí|耆,qí|鯕,qí|袳,qǐ|乞,qǐ|邔,qǐ|企,qǐ|屺,qǐ|岂,qǐ|芑,qǐ|启,qǐ|呇,qǐ|杞,qǐ|玘,qǐ|盀,qǐ|唘,qǐ|豈,qǐ|起,qǐ|啓,qǐ|啔,qǐ|啟,qǐ|绮,qǐ|棨,qǐ|綮,qǐ|綺,qǐ|諬,qǐ|簯,qǐ|闙,qǐ|梩,qǐ|婍,qǐ|鼜,qì|悽,qì|槭,qì|气,qì|讫,qì|気,qì|汔,qì|迄,qì|弃,qì|汽,qì|芞,qì|呮,qì|泣,qì|炁,qì|盵,qì|咠,qì|契,qì|砌,qì|栔,qì|氣,qì|訖,qì|唭,qì|夡,qì|棄,qì|湆,qì|湇,qì|葺,qì|碛,qì|摖,qì|暣,qì|甈,qì|碶,qì|噐,qì|憇,qì|器,qì|憩,qì|磜,qì|磧,qì|磩,qì|罊,qì|趞,qì|洓,qì|慼,qì|欫,qì|掐,qiā|葜,qiā|愘,qiā|搳,qiā|拤,qiá|跒,qiǎ|酠,qiǎ|鞐,qiǎ|圶,qià|冾,qià|恰,qià|洽,qià|殎,qià|硈,qià|髂,qià|磍,qià|帢,qià|千,qiān|仟,qiān|阡,qiān|圱,qiān|圲,qiān|奷,qiān|扦,qiān|汘,qiān|芊,qiān|迁,qiān|佥,qiān|岍,qiān|杄,qiān|汧,qiān|茾,qiān|竏,qiān|臤,qiān|钎,qiān|拪,qiān|牵,qiān|粁,qiān|悭,qiān|蚈,qiān|铅,qiān|牽,qiān|釺,qiān|谦,qiān|鈆,qiān|僉,qiān|愆,qiān|签,qiān|鉛,qiān|骞,qiān|鹐,qiān|慳,qiān|搴,qiān|撁,qiān|箞,qiān|諐,qiān|遷,qiān|褰,qiān|謙,qiān|顅,qiān|檶,qiān|攐,qiān|攑,qiān|櫏,qiān|簽,qiān|鵮,qiān|攓,qiān|騫,qiān|鬜,qiān|鬝,qiān|籤,qiān|韆,qiān|鋟,qiān|扡,qiān|杴,qiān|孅,qiān|藖,qiān|谸,qiān|鏲,qiān|朁,qián|岒,qián|忴,qián|扲,qián|拑,qián|前,qián|荨,qián|钤,qián|歬,qián|虔,qián|钱,qián|钳,qián|乾,qián|掮,qián|軡,qián|媊,qián|鈐,qián|鉗,qián|榩,qián|箝,qián|潜,qián|羬,qián|橬,qián|錢,qián|黔,qián|鎆,qián|騝,qián|濳,qián|騚,qián|灊,qián|籖,qián|鰬,qián|潛,qián|蚙,qián|煔,qián|燂,qián|葴,qián|鍼,qián|墘,qián|浅,qiǎn|肷,qiǎn|淺,qiǎn|嵰,qiǎn|遣,qiǎn|槏,qiǎn|膁,qiǎn|蜸,qiǎn|谴,qiǎn|缱,qiǎn|譴,qiǎn|鑓,qiǎn|繾,qiǎn|欠,qiàn|刋,qiàn|伣,qiàn|芡,qiàn|俔,qiàn|茜,qiàn|倩,qiàn|悓,qiàn|堑,qiàn|嵌,qiàn|棈,qiàn|椠,qiàn|嗛,qiàn|皘,qiàn|蒨,qiàn|塹,qiàn|歉,qiàn|綪,qiàn|蔳,qiàn|儙,qiàn|槧,qiàn|篏,qiàn|輤,qiàn|篟,qiàn|壍,qiàn|嬱,qiàn|縴,qiàn|廞,qiàn|鸧,qiāng|鶬,qiāng|羌,qiāng|戕,qiāng|戗,qiāng|斨,qiāng|枪,qiāng|玱,qiāng|猐,qiāng|琷,qiāng|跄,qiāng|嗴,qiāng|獇,qiāng|腔,qiāng|溬,qiāng|蜣,qiāng|锖,qiāng|嶈,qiāng|戧,qiāng|槍,qiāng|牄,qiāng|瑲,qiāng|锵,qiāng|篬,qiāng|錆,qiāng|蹌,qiāng|镪,qiāng|蹡,qiāng|鏘,qiāng|鏹,qiāng|啌,qiāng|鎗,qiāng|強,qiáng|强,qiáng|墙,qiáng|嫱,qiáng|蔷,qiáng|樯,qiáng|漒,qiáng|墻,qiáng|嬙,qiáng|廧,qiáng|薔,qiáng|檣,qiáng|牆,qiáng|謒,qiáng|艢,qiáng|蘠,qiáng|抢,qiǎng|羟,qiǎng|搶,qiǎng|羥,qiǎng|墏,qiǎng|摤,qiǎng|繈,qiǎng|襁,qiǎng|繦,qiǎng|嗆,qiàng|炝,qiàng|唴,qiàng|羻,qiàng|呛,qiàng|熗,qiàng|悄,qiāo|硗,qiāo|郻,qiāo|跷,qiāo|鄡,qiāo|鄥,qiāo|劁,qiāo|敲,qiāo|踍,qiāo|锹,qiāo|碻,qiāo|頝,qiāo|墽,qiāo|幧,qiāo|橇,qiāo|燆,qiāo|缲,qiāo|鍫,qiāo|鍬,qiāo|繰,qiāo|趬,qiāo|鐰,qiāo|鞽,qiāo|塙,qiāo|毃,qiāo|鏒,qiāo|橾,qiāo|喿,qiāo|蹺,qiāo|峤,qiáo|嶠,qiáo|乔,qiáo|侨,qiáo|荍,qiáo|荞,qiáo|桥,qiáo|硚,qiáo|菬,qiáo|喬,qiáo|睄,qiáo|僑,qiáo|槗,qiáo|谯,qiáo|嘺,qiáo|憔,qiáo|蕎,qiáo|鞒,qiáo|樵,qiáo|橋,qiáo|犞,qiáo|癄,qiáo|瞧,qiáo|礄,qiáo|藮,qiáo|譙,qiáo|鐈,qiáo|墧,qiáo|顦,qiáo|磽,qiǎo|巧,qiǎo|愀,qiǎo|髜,qiǎo|偢,qiào|墝,qiào|俏,qiào|诮,qiào|陗,qiào|峭,qiào|帩,qiào|窍,qiào|翘,qiào|誚,qiào|髚,qiào|僺,qiào|撬,qiào|鞘,qiào|韒,qiào|竅,qiào|翹,qiào|鞩,qiào|躈,qiào|踃,qiào|切,qiē|苆,qiē|癿,qié|茄,qié|聺,qié|且,qiě|詧,qiè|慊,qiè|厒,qiè|怯,qiè|匧,qiè|窃,qiè|倿,qiè|悏,qiè|挈,qiè|惬,qiè|笡,qiè|愜,qiè|朅,qiè|箧,qiè|緁,qiè|锲,qiè|篋,qiè|踥,qiè|穕,qiè|藒,qiè|鍥,qiè|鯜,qiè|鐑,qiè|竊,qiè|籡,qiè|帹,qiè|郄,qiè|郤,qiè|稧,qiè|妾,qiè|亲,qīn|侵,qīn|钦,qīn|衾,qīn|菳,qīn|媇,qīn|嵚,qīn|綅,qīn|誛,qīn|嶔,qīn|親,qīn|顉,qīn|駸,qīn|鮼,qīn|寴,qīn|欽,qīn|骎,qīn|鈂,qín|庈,qín|芩,qín|芹,qín|埁,qín|珡,qín|矝,qín|秦,qín|耹,qín|菦,qín|捦,qín|琴,qín|琹,qín|禽,qín|鈙,qín|雂,qín|勤,qín|嗪,qín|嫀,qín|靲,qín|噙,qín|擒,qín|鳹,qín|懄,qín|檎,qín|澿,qín|瘽,qín|螓,qín|懃,qín|蠄,qín|鬵,qín|溱,qín|坅,qǐn|昑,qǐn|笉,qǐn|梫,qǐn|赾,qǐn|寑,qǐn|锓,qǐn|寝,qǐn|寢,qǐn|螼,qǐn|儭,qìn|櫬,qìn|吢,qìn|吣,qìn|抋,qìn|沁,qìn|唚,qìn|菣,qìn|搇,qìn|撳,qìn|瀙,qìn|藽,qìn|鈊,qìn|揿,qìn|鶄,qīng|青,qīng|氢,qīng|轻,qīng|倾,qīng|卿,qīng|郬,qīng|圊,qīng|埥,qīng|氫,qīng|淸,qīng|清,qīng|軽,qīng|傾,qīng|廎,qīng|蜻,qīng|輕,qīng|鲭,qīng|鯖,qīng|鑋,qīng|庼,qīng|漀,qīng|靘,qīng|夝,qíng|甠,qíng|勍,qíng|情,qíng|硘,qíng|晴,qíng|棾,qíng|氰,qíng|暒,qíng|樈,qíng|擎,qíng|檠,qíng|黥,qíng|殑,qíng|苘,qǐng|顷,qǐng|请,qǐng|頃,qǐng|請,qǐng|檾,qǐng|謦,qǐng|庆,qìng|摐,chuāng|牀,chuáng|磢,chuǎng|刱,chuàng|吹,chuī|糚,zhuāng|庒,zhuāng|漴,zhuàng|丬,zhuàng|壮,zhuàng|凊,qìng|掅,qìng|碃,qìng|箐,qìng|慶,qìng|磬,qìng|罄,qìng|櫦,qìng|濪,qìng|藭,qiong|跫,qióng|銎,qióng|卭,qióng|邛,qióng|穷,qióng|穹,qióng|茕,qióng|桏,qióng|笻,qióng|筇,qióng|赹,qióng|惸,qióng|焪,qióng|焭,qióng|琼,qióng|蛩,qióng|蛬,qióng|煢,qióng|熍,qióng|睘,qióng|窮,qióng|儝,qióng|憌,qióng|橩,qióng|瓊,qióng|竆,qióng|嬛,qióng|琁,qióng|藑,qióng|湫,qiū|丘,qiū|丠,qiū|邱,qiū|坵,qiū|恘,qiū|秋,qiū|秌,qiū|寈,qiū|蚯,qiū|媝,qiū|楸,qiū|鹙,qiū|篍,qiū|緧,qiū|蝵,qiū|穐,qiū|趥,qiū|鳅,qiū|蟗,qiū|鞦,qiū|鞧,qiū|蘒,qiū|鰌,qiū|鰍,qiū|鱃,qiū|龝,qiū|逎,qiū|櫹,qiū|鶖,qiū|叴,qiú|囚,qiú|扏,qiú|犰,qiú|玌,qiú|肍,qiú|求,qiú|虬,qiú|泅,qiú|虯,qiú|俅,qiú|觓,qiú|訅,qiú|酋,qiú|唒,qiú|浗,qiú|紌,qiú|莍,qiú|逑,qiú|釚,qiú|梂,qiú|殏,qiú|毬,qiú|球,qiú|釻,qiú|崷,qiú|巯,qiú|湭,qiú|皳,qiú|盚,qiú|遒,qiú|煪,qiú|絿,qiú|蛷,qiú|裘,qiú|巰,qiú|觩,qiú|賕,qiú|璆,qiú|銶,qiú|醔,qiú|鮂,qiú|鼽,qiú|鯄,qiú|鵭,qiú|蠤,qiú|鰽,qiú|厹,qiú|赇,qiú|搝,qiǔ|糗,qiǔ|趍,qū|匚,qū|区,qū|伹,qū|匤,qū|岖,qū|诎,qū|阹,qū|驱,qū|屈,qū|岨,qū|岴,qū|抾,qū|浀,qū|祛,qū|胠,qū|袪,qū|區,qū|蛆,qū|躯,qū|筁,qū|粬,qū|蛐,qū|詘,qū|趋,qū|嶇,qū|駆,qū|憈,qū|駈,qū|麹,qū|髷,qū|趨,qū|麯,qū|軀,qū|麴,qū|黢,qū|驅,qū|鰸,qū|鱋,qū|紶,qū|厺,qū|佉,qū|跼,qú|瞿,qú|佢,qú|劬,qú|斪,qú|朐,qú|胊,qú|菃,qú|衐,qú|鸲,qú|淭,qú|渠,qú|絇,qú|葋,qú|蕖,qú|璖,qú|磲,qú|璩,qú|鼩,qú|蘧,qú|灈,qú|戵,qú|欋,qú|氍,qú|臞,qú|癯,qú|蠷,qú|衢,qú|躣,qú|蠼,qú|鑺,qú|臒,qú|蟝,qú|曲,qǔ|取,qǔ|娶,qǔ|詓,qǔ|竬,qǔ|龋,qǔ|齲,qǔ|去,qù|刞,qù|耝,qù|阒,qù|觑,qù|趣,qù|閴,qù|麮,qù|闃,qù|覰,qù|覷,qù|鼁,qù|覻,qù|迲,qù|峑,quān|恮,quān|悛,quān|圈,quān|駩,quān|騡,quān|鐉,quān|腃,quān|全,quán|权,quán|佺,quán|诠,quán|姾,quán|泉,quán|洤,quán|荃,quán|拳,quán|辁,quán|婘,quán|痊,quán|硂,quán|铨,quán|湶,quán|犈,quán|筌,quán|絟,quán|葲,quán|搼,quán|楾,quán|瑔,quán|觠,quán|詮,quán|跧,quán|輇,quán|蜷,quán|銓,quán|権,quán|縓,quán|醛,quán|闎,quán|鳈,quán|鬈,quán|巏,quán|鰁,quán|權,quán|齤,quán|颧,quán|顴,quán|灥,quán|譔,quán|牷,quán|孉,quán|犬,quǎn|甽,quǎn|畎,quǎn|烇,quǎn|绻,quǎn|綣,quǎn|虇,quǎn|劝,quàn|券,quàn|巻,quàn|牶,quàn|椦,quàn|勧,quàn|勸,quàn|炔,quē|缺,quē|蒛,quē|瘸,qué|却,què|卻,què|崅,què|悫,què|雀,què|确,què|阕,què|皵,què|碏,què|阙,què|鹊,què|愨,què|榷,què|慤,què|確,què|燩,què|闋,què|闕,què|鵲,què|礭,què|殻,què|埆,què|踆,qūn|夋,qūn|囷,qūn|峮,qūn|逡,qūn|帬,qún|裙,qún|羣,qún|群,qún|裠,qún|亽,ra|罖,ra|囕,ram|呥,rán|肰,rán|衻,rán|袇,rán|蚦,rán|袡,rán|蚺,rán|然,rán|髥,rán|嘫,rán|髯,rán|燃,rán|繎,rán|冄,rán|冉,rǎn|姌,rǎn|苒,rǎn|染,rǎn|珃,rǎn|媣,rǎn|蒅,rǎn|孃,ráng|穣,ráng|獽,ráng|禳,ráng|瓤,ráng|穰,ráng|躟,ráng|壌,rǎng|嚷,rǎng|壤,rǎng|攘,rǎng|爙,rǎng|让,ràng|懹,ràng|譲,ràng|讓,ràng|荛,ráo|饶,ráo|桡,ráo|橈,ráo|襓,ráo|饒,ráo|犪,ráo|嬈,ráo|娆,ráo|扰,rǎo|隢,rǎo|擾,rǎo|遶,rǎo|绕,rào|繞,rào|惹,rě|热,rè|熱,rè|渃,rè|綛,ren|人,rén|仁,rén|壬,rén|忈,rén|朲,rén|忎,rén|秂,rén|芢,rén|鈓,rén|魜,rén|銋,rén|鵀,rén|姙,rén|忍,rěn|荏,rěn|栠,rěn|栣,rěn|荵,rěn|秹,rěn|稔,rěn|躵,rěn|刃,rèn|刄,rèn|认,rèn|仞,rèn|仭,rèn|讱,rèn|任,rèn|屻,rèn|扨,rèn|纫,rèn|妊,rèn|牣,rèn|纴,rèn|肕,rèn|轫,rèn|韧,rèn|饪,rèn|紉,rèn|衽,rèn|紝,rèn|訒,rèn|軔,rèn|梕,rèn|袵,rèn|絍,rèn|靭,rèn|靱,rèn|韌,rèn|飪,rèn|認,rèn|餁,rèn|扔,rēng|仍,réng|辸,réng|礽,réng|芿,réng|日,rì|驲,rì|囸,rì|釰,rì|鈤,rì|馹,rì|戎,róng|肜,róng|栄,róng|狨,róng|绒,róng|茙,róng|茸,róng|荣,róng|容,róng|峵,róng|毧,róng|烿,róng|嵘,róng|絨,róng|羢,róng|嫆,róng|搈,róng|摉,róng|榵,róng|溶,róng|蓉,róng|榕,róng|榮,róng|熔,róng|瑢,róng|穁,róng|蝾,róng|褣,róng|镕,róng|氄,róng|縙,róng|融,róng|螎,róng|駥,róng|嬫,róng|嶸,róng|爃,róng|鎔,róng|瀜,róng|蠑,róng|媶,róng|曧,róng|冗,rǒng|宂,rǒng|傇,rǒng|穃,ròng|禸,róu|柔,róu|粈,róu|媃,róu|揉,róu|渘,róu|葇,róu|瑈,róu|腬,róu|糅,róu|蹂,róu|輮,róu|鍒,róu|鞣,róu|瓇,róu|騥,róu|鰇,róu|鶔,róu|楺,rǒu|煣,rǒu|韖,rǒu|肉,ròu|宍,ròu|嶿,rū|如,rú|侞,rú|帤,rú|茹,rú|桇,rú|袽,rú|铷,rú|渪,rú|筎,rú|銣,rú|蕠,rú|儒,rú|鴑,rú|嚅,rú|孺,rú|濡,rú|薷,rú|鴽,rú|曘,rú|燸,rú|襦,rú|蠕,rú|颥,rú|醹,rú|顬,rú|偄,rú|鱬,rú|汝,rǔ|肗,rǔ|乳,rǔ|辱,rǔ|鄏,rǔ|擩,rǔ|入,rù|扖,rù|込,rù|杁,rù|洳,rù|嗕,rù|媷,rù|溽,rù|缛,rù|蓐,rù|鳰,rù|褥,rù|縟,rù|壖,ruán|阮,ruǎn|朊,ruǎn|软,ruǎn|軟,ruǎn|碝,ruǎn|緛,ruǎn|蝡,ruǎn|瓀,ruǎn|礝,ruǎn|瑌,ruǎn|撋,ruí|桵,ruí|甤,ruí|緌,ruí|蕤,ruí|蕊,ruǐ|橤,ruǐ|繠,ruǐ|蘂,ruǐ|蘃,ruǐ|惢,ruǐ|芮,ruì|枘,ruì|蚋,ruì|锐,ruì|瑞,ruì|睿,ruì|銳,ruì|叡,ruì|壡,ruì|润,rùn|閏,rùn|閠,rùn|潤,rùn|橍,rùn|闰,rùn|叒,ruò|若,ruò|偌,ruò|弱,ruò|鄀,ruò|焫,ruò|楉,ruò|嵶,ruò|蒻,ruò|箬,ruò|爇,ruò|鰙,ruò|鰯,ruò|鶸,ruò|仨,sā|桬,sā|撒,sā|洒,sǎ|訯,sǎ|靸,sǎ|灑,sǎ|卅,sà|飒,sà|脎,sà|萨,sà|隡,sà|馺,sà|颯,sà|薩,sà|櫒,sà|栍,saeng|毢,sāi|塞,sāi|毸,sāi|腮,sāi|嘥,sāi|噻,sāi|鳃,sāi|顋,sāi|鰓,sāi|嗮,sǎi|赛,sài|僿,sài|賽,sài|簺,sài|虄,sal|厁,san|壭,san|三,sān|弎,sān|叁,sān|毵,sān|毶,sān|毿,sān|犙,sān|鬖,sān|糂,sān|糝,sān|糣,sān|彡,sān|氵,sān|伞,sǎn|傘,sǎn|馓,sǎn|橵,sǎn|糤,sǎn|繖,sǎn|饊,sǎn|散,sàn|俕,sàn|閐,sàn|潵,sàn|桒,sāng|桑,sāng|槡,sāng|嗓,sǎng|搡,sǎng|褬,sǎng|颡,sǎng|鎟,sǎng|顙,sǎng|磉,sǎng|丧,sàng|喪,sàng|掻,sāo|搔,sāo|溞,sāo|骚,sāo|缫,sāo|繅,sāo|鳋,sāo|颾,sāo|騒,sāo|騷,sāo|鰠,sāo|鱢,sāo|扫,sǎo|掃,sǎo|嫂,sǎo|臊,sào|埽,sào|瘙,sào|氉,sào|矂,sào|髞,sào|色,sè|涩,sè|啬,sè|渋,sè|铯,sè|歮,sè|嗇,sè|瑟,sè|歰,sè|銫,sè|澁,sè|懎,sè|擌,sè|濇,sè|濏,sè|瘷,sè|穑,sè|澀,sè|璱,sè|瀒,sè|穡,sè|繬,sè|穯,sè|轖,sè|鏼,sè|譅,sè|飋,sè|愬,sè|鎍,sè|溹,sè|栜,sè|裇,sed|聓,sei|森,sēn|僧,sēng|鬙,sēng|閪,seo|縇,seon|杀,shā|沙,shā|纱,shā|乷,shā|刹,shā|砂,shā|唦,shā|挱,shā|殺,shā|猀,shā|紗,shā|莎,shā|铩,shā|痧,shā|硰,shā|蔱,shā|裟,shā|樧,shā|魦,shā|鲨,shā|閷,shā|鯊,shā|鯋,shā|繺,shā|賖,shā|啥,shá|傻,shǎ|儍,shǎ|繌,shǎ|倽,shà|唼,shà|萐,shà|歃,shà|煞,shà|翜,shà|翣,shà|閯,shà|霎,shà|厦,shà|廈,shà|筛,shāi|篩,shāi|簁,shāi|簛,shāi|酾,shāi|釃,shāi|摋,shǎi|晒,shài|曬,shài|纔,shān|穇,shān|凵,shān|襂,shān|山,shān|邖,shān|圸,shān|删,shān|杉,shān|杣,shān|芟,shān|姍,shān|姗,shān|衫,shān|钐,shān|埏,shān|狦,shān|珊,shān|舢,shān|痁,shān|軕,shān|笘,shān|釤,shān|閊,shān|跚,shān|剼,shān|搧,shān|嘇,shān|幓,shān|煽,shān|潸,shān|澘,shān|曑,shān|檆,shān|膻,shān|鯅,shān|羴,shān|羶,shān|炶,shān|苫,shān|柵,shān|栅,shān|刪,shān|闪,shǎn|陕,shǎn|陝,shǎn|閃,shǎn|晱,shǎn|睒,shǎn|熌,shǎn|覢,shǎn|曏,shǎn|笧,shàn|讪,shàn|汕,shàn|疝,shàn|扇,shàn|訕,shàn|赸,shàn|傓,shàn|善,shàn|椫,shàn|銏,shàn|骟,shàn|僐,shàn|鄯,shàn|缮,shàn|嬗,shàn|擅,shàn|敾,shàn|樿,shàn|膳,shàn|磰,shàn|謆,shàn|赡,shàn|繕,shàn|蟮,shàn|譱,shàn|贍,shàn|鐥,shàn|饍,shàn|騸,shàn|鳝,shàn|灗,shàn|鱔,shàn|鱣,shàn|墡,shàn|裳,shang|塲,shāng|伤,shāng|殇,shāng|商,shāng|觞,shāng|傷,shāng|墒,shāng|慯,shāng|滳,shāng|蔏,shāng|殤,shāng|熵,shāng|螪,shāng|觴,shāng|謪,shāng|鬺,shāng|坰,shǎng|垧,shǎng|晌,shǎng|赏,shǎng|賞,shǎng|鑜,shǎng|丄,shàng|上,shàng|仩,shàng|尚,shàng|恦,shàng|绱,shàng|緔,shàng|弰,shāo|捎,shāo|梢,shāo|烧,shāo|焼,shāo|稍,shāo|筲,shāo|艄,shāo|蛸,shāo|輎,shāo|蕱,shāo|燒,shāo|髾,shāo|鮹,shāo|娋,shāo|旓,shāo|杓,sháo|勺,sháo|芍,sháo|柖,sháo|玿,sháo|韶,sháo|少,shǎo|劭,shào|卲,shào|邵,shào|绍,shào|哨,shào|袑,shào|紹,shào|潲,shào|奢,shē|猞,shē|赊,shē|輋,shē|賒,shē|檨,shē|畲,shē|舌,shé|佘,shé|蛇,shé|蛥,shé|磼,shé|折,shé|舍,shě|捨,shě|厍,shè|设,shè|社,shè|舎,shè|厙,shè|射,shè|涉,shè|涻,shè|設,shè|赦,shè|弽,shè|慑,shè|摄,shè|滠,shè|慴,shè|摵,shè|蔎,shè|韘,shè|騇,shè|懾,shè|攝,shè|麝,shè|欇,shè|挕,shè|蠂,shè|堔,shen|叄,shēn|糁,shēn|申,shēn|屾,shēn|扟,shēn|伸,shēn|身,shēn|侁,shēn|呻,shēn|妽,shēn|籶,shēn|绅,shēn|诜,shēn|柛,shēn|氠,shēn|珅,shēn|穼,shēn|籸,shēn|娠,shēn|峷,shēn|甡,shēn|眒,shēn|砷,shēn|深,shēn|紳,shēn|兟,shēn|椮,shēn|葠,shēn|裑,shēn|訷,shēn|罧,shēn|蓡,shēn|詵,shēn|甧,shēn|蔘,shēn|燊,shēn|薓,shēn|駪,shēn|鲹,shēn|鯓,shēn|鵢,shēn|鯵,shēn|鰺,shēn|莘,shēn|叅,shēn|神,shén|榊,shén|鰰,shén|棯,shěn|槮,shěn|邥,shěn|弞,shěn|沈,shěn|审,shěn|矤,shěn|矧,shěn|谂,shěn|谉,shěn|婶,shěn|渖,shěn|訠,shěn|審,shěn|頣,shěn|魫,shěn|曋,shěn|瞫,shěn|嬸,shěn|覾,shěn|讅,shěn|哂,shěn|肾,shèn|侺,shèn|昚,shèn|甚,shèn|胂,shèn|眘,shèn|渗,shèn|祳,shèn|脤,shèn|腎,shèn|愼,shèn|慎,shèn|瘆,shèn|蜃,shèn|滲,shèn|鋠,shèn|瘮,shèn|葚,shèn|升,shēng|生,shēng|阩,shēng|呏,shēng|声,shēng|斘,shēng|昇,shēng|枡,shēng|泩,shēng|苼,shēng|殅,shēng|牲,shēng|珄,shēng|竔,shēng|陞,shēng|曻,shēng|陹,shēng|笙,shēng|湦,shēng|焺,shēng|甥,shēng|鉎,shēng|聲,shēng|鍟,shēng|鵿,shēng|鼪,shēng|绳,shéng|縄,shéng|憴,shéng|繩,shéng|譝,shéng|省,shěng|眚,shěng|偗,shěng|渻,shěng|胜,shèng|圣,shèng|晟,shèng|晠,shèng|剰,shèng|盛,shèng|剩,shèng|勝,shèng|貹,shèng|嵊,shèng|聖,shèng|墭,shèng|榺,shèng|蕂,shèng|橳,shèng|賸,shèng|鳾,shi|觢,shi|尸,shī|师,shī|呞,shī|虱,shī|诗,shī|邿,shī|鸤,shī|屍,shī|施,shī|浉,shī|狮,shī|師,shī|絁,shī|湤,shī|湿,shī|葹,shī|溮,shī|溼,shī|獅,shī|蒒,shī|蓍,shī|詩,shī|瑡,shī|鳲,shī|蝨,shī|鲺,shī|濕,shī|鍦,shī|鯴,shī|鰤,shī|鶳,shī|襹,shī|籭,shī|魳,shī|失,shī|褷,shī|匙,shí|十,shí|什,shí|石,shí|辻,shí|佦,shí|时,shí|竍,shí|识,shí|实,shí|実,shí|旹,shí|飠,shí|峕,shí|拾,shí|炻,shí|祏,shí|蚀,shí|食,shí|埘,shí|寔,shí|湜,shí|遈,shí|塒,shí|嵵,shí|溡,shí|鉐,shí|實,shí|榯,shí|蝕,shí|鉽,shí|篒,shí|鲥,shí|鮖,shí|鼫,shí|識,shí|鼭,shí|鰣,shí|時,shí|史,shǐ|矢,shǐ|乨,shǐ|豕,shǐ|使,shǐ|始,shǐ|驶,shǐ|兘,shǐ|屎,shǐ|榁,shǐ|鉂,shǐ|駛,shǐ|笶,shǐ|饣,shì|莳,shì|蒔,shì|士,shì|氏,shì|礻,shì|世,shì|丗,shì|仕,shì|市,shì|示,shì|卋,shì|式,shì|事,shì|侍,shì|势,shì|呩,shì|视,shì|试,shì|饰,shì|冟,shì|室,shì|恀,shì|恃,shì|拭,shì|枾,shì|柿,shì|眂,shì|贳,shì|适,shì|栻,shì|烒,shì|眎,shì|眡,shì|舐,shì|轼,shì|逝,shì|铈,shì|視,shì|釈,shì|弑,shì|揓,shì|谥,shì|貰,shì|释,shì|勢,shì|嗜,shì|弒,shì|煶,shì|睗,shì|筮,shì|試,shì|軾,shì|鈰,shì|鉃,shì|飾,shì|舓,shì|誓,shì|適,shì|奭,shì|噬,shì|嬕,shì|澨,shì|諡,shì|遾,shì|螫,shì|簭,shì|籂,shì|襫,shì|釋,shì|鰘,shì|佀,shì|鎩,shì|是,shì|収,shōu|收,shōu|手,shǒu|守,shǒu|垨,shǒu|首,shǒu|艏,shǒu|醻,shòu|寿,shòu|受,shòu|狩,shòu|兽,shòu|售,shòu|授,shòu|绶,shòu|痩,shòu|膄,shòu|壽,shòu|瘦,shòu|綬,shòu|夀,shòu|獣,shòu|獸,shòu|鏉,shòu|书,shū|殳,shū|抒,shū|纾,shū|叔,shū|枢,shū|姝,shū|柕,shū|倏,shū|倐,shū|書,shū|殊,shū|紓,shū|掓,shū|梳,shū|淑,shū|焂,shū|菽,shū|軗,shū|鄃,shū|疎,shū|疏,shū|舒,shū|摅,shū|毹,shū|毺,shū|綀,shū|输,shū|踈,shū|樞,shū|蔬,shū|輸,shū|鮛,shū|瀭,shū|鵨,shū|陎,shū|尗,shú|秫,shú|婌,shú|孰,shú|赎,shú|塾,shú|熟,shú|璹,shú|贖,shú|暑,shǔ|黍,shǔ|署,shǔ|鼠,shǔ|鼡,shǔ|蜀,shǔ|潻,shǔ|薯,shǔ|曙,shǔ|癙,shǔ|糬,shǔ|籔,shǔ|蠴,shǔ|鱰,shǔ|属,shǔ|屬,shǔ|鱪,shǔ|丨,shù|术,shù|戍,shù|束,shù|沭,shù|述,shù|怷,shù|树,shù|竖,shù|荗,shù|恕,shù|庶,shù|庻,shù|絉,shù|蒁,shù|術,shù|裋,shù|数,shù|竪,shù|腧,shù|墅,shù|漱,shù|潄,shù|數,shù|豎,shù|樹,shù|濖,shù|錰,shù|鏣,shù|鶐,shù|虪,shù|捒,shù|忄,shù|澍,shù|刷,shuā|唰,shuā|耍,shuǎ|誜,shuà|缞,shuāi|縗,shuāi|衰,shuāi|摔,shuāi|甩,shuǎi|帅,shuài|帥,shuài|蟀,shuài|闩,shuān|拴,shuān|閂,shuān|栓,shuān|涮,shuàn|腨,shuàn|双,shuāng|脽,shuí|誰,shuí|水,shuǐ|氺,shuǐ|閖,shuǐ|帨,shuì|涗,shuì|涚,shuì|稅,shuì|税,shuì|裞,shuì|説,shuì|睡,shuì|吮,shǔn|顺,shùn|舜,shùn|順,shùn|蕣,shùn|橓,shùn|瞚,shùn|瞤,shùn|瞬,shùn|鬊,shùn|说,shuō|說,shuō|妁,shuò|烁,shuò|朔,shuò|铄,shuò|欶,shuò|硕,shuò|矟,shuò|搠,shuò|蒴,shuò|槊,shuò|碩,shuò|爍,shuò|鑠,shuò|洬,shuò|燿,shuò|鎙,shuò|愢,sī|厶,sī|丝,sī|司,sī|糹,sī|私,sī|咝,sī|泀,sī|俬,sī|思,sī|恖,sī|鸶,sī|媤,sī|斯,sī|絲,sī|缌,sī|蛳,sī|楒,sī|禗,sī|鉰,sī|飔,sī|凘,sī|厮,sī|榹,sī|禠,sī|罳,sī|锶,sī|嘶,sī|噝,sī|廝,sī|撕,sī|澌,sī|緦,sī|蕬,sī|螄,sī|鍶,sī|蟖,sī|蟴,sī|颸,sī|騦,sī|鐁,sī|鷥,sī|鼶,sī|鷉,sī|銯,sī|死,sǐ|灬,sì|巳,sì|亖,sì|四,sì|罒,sì|寺,sì|汜,sì|伺,sì|似,sì|姒,sì|泤,sì|祀,sì|価,sì|孠,sì|泗,sì|饲,sì|驷,sì|俟,sì|娰,sì|柶,sì|牭,sì|洍,sì|涘,sì|肂,sì|飤,sì|笥,sì|耜,sì|釲,sì|竢,sì|覗,sì|嗣,sì|肆,sì|貄,sì|鈻,sì|飼,sì|禩,sì|駟,sì|儩,sì|瀃,sì|兕,sì|蕼,sì|螦,so|乺,sol|忪,sōng|松,sōng|枀,sōng|枩,sōng|娀,sōng|柗,sōng|倯,sōng|凇,sōng|梥,sōng|崧,sōng|庺,sōng|淞,sōng|菘,sōng|嵩,sōng|硹,sōng|蜙,sōng|憽,sōng|檧,sōng|濍,sōng|怂,sǒng|悚,sǒng|耸,sǒng|竦,sǒng|愯,sǒng|嵷,sǒng|慫,sǒng|聳,sǒng|駷,sǒng|鬆,sòng|讼,sòng|宋,sòng|诵,sòng|送,sòng|颂,sòng|訟,sòng|頌,sòng|誦,sòng|餸,sòng|鎹,sòng|凁,sōu|捜,sōu|鄋,sōu|嗖,sōu|廀,sōu|廋,sōu|搜,sōu|溲,sōu|獀,sōu|蒐,sōu|蓃,sōu|馊,sōu|飕,sōu|摗,sōu|锼,sōu|螋,sōu|醙,sōu|鎪,sōu|餿,sōu|颼,sōu|騪,sōu|叜,sōu|艘,sōu|叟,sǒu|傁,sǒu|嗾,sǒu|瞍,sǒu|擞,sǒu|薮,sǒu|擻,sǒu|藪,sǒu|櫢,sǒu|嗽,sòu|瘶,sòu|苏,sū|甦,sū|酥,sū|稣,sū|窣,sū|穌,sū|鯂,sū|蘇,sū|蘓,sū|櫯,sū|囌,sū|卹,sū|俗,sú|玊,sù|诉,sù|泝,sù|肃,sù|涑,sù|珟,sù|素,sù|速,sù|殐,sù|粛,sù|骕,sù|傃,sù|粟,sù|訴,sù|谡,sù|嗉,sù|塐,sù|塑,sù|嫊,sù|愫,sù|溯,sù|溸,sù|肅,sù|遡,sù|鹔,sù|僳,sù|榡,sù|蔌,sù|觫,sù|趚,sù|遬,sù|憟,sù|樎,sù|樕,sù|潥,sù|鋉,sù|餗,sù|縤,sù|璛,sù|簌,sù|藗,sù|謖,sù|蹜,sù|驌,sù|鱐,sù|鷫,sù|埣,sù|夙,sù|膆,sù|狻,suān|痠,suān|酸,suān|匴,suǎn|祘,suàn|笇,suàn|筭,suàn|蒜,suàn|算,suàn|夊,suī|芕,suī|虽,suī|倠,suī|哸,suī|荽,suī|荾,suī|眭,suī|滖,suī|睢,suī|濉,suī|鞖,suī|雖,suī|簑,suī|绥,suí|隋,suí|随,suí|遀,suí|綏,suí|隨,suí|瓍,suí|遂,suí|瀡,suǐ|髄,suǐ|髓,suǐ|亗,suì|岁,suì|砕,suì|谇,suì|歲,suì|歳,suì|煫,suì|碎,suì|隧,suì|嬘,suì|澻,suì|穂,suì|誶,suì|賥,suì|檖,suì|燧,suì|璲,suì|禭,suì|穗,suì|穟,suì|襚,suì|邃,suì|旞,suì|繐,suì|繸,suì|鐆,suì|鐩,suì|祟,suì|譢,suì|孙,sūn|狲,sūn|荪,sūn|孫,sūn|飧,sūn|搎,sūn|猻,sūn|蓀,sūn|槂,sūn|蕵,sūn|薞,sūn|畃,sún|损,sǔn|笋,sǔn|隼,sǔn|筍,sǔn|損,sǔn|榫,sǔn|箰,sǔn|鎨,sǔn|巺,sùn|潠,sùn|嗍,suō|唆,suō|娑,suō|莏,suō|傞,suō|桫,suō|梭,suō|睃,suō|嗦,suō|羧,suō|蓑,suō|摍,suō|缩,suō|趖,suō|簔,suō|縮,suō|髿,suō|鮻,suō|挲,suō|所,suǒ|唢,suǒ|索,suǒ|琐,suǒ|琑,suǒ|锁,suǒ|嗩,suǒ|暛,suǒ|溑,suǒ|瑣,suǒ|鎖,suǒ|鎻,suǒ|鏁,suǒ|嵗,suò|蜶,suò|逤,suò|侤,ta|澾,ta|她,tā|他,tā|它,tā|祂,tā|咜,tā|趿,tā|铊,tā|塌,tā|榙,tā|溻,tā|鉈,tā|褟,tā|遢,tā|蹹,tá|塔,tǎ|墖,tǎ|獭,tǎ|鳎,tǎ|獺,tǎ|鰨,tǎ|沓,tà|挞,tà|狧,tà|闼,tà|崉,tà|涾,tà|遝,tà|阘,tà|榻,tà|毾,tà|禢,tà|撻,tà|誻,tà|踏,tà|嚃,tà|錔,tà|嚺,tà|濌,tà|蹋,tà|鞜,tà|闒,tà|鞳,tà|闥,tà|譶,tà|躢,tà|傝,tà|襨,tae|漦,tāi|咍,tāi|囼,tāi|孡,tāi|胎,tāi|駘,tāi|檯,tāi|斄,tái|台,tái|邰,tái|坮,tái|苔,tái|炱,tái|炲,tái|菭,tái|跆,tái|鲐,tái|箈,tái|臺,tái|颱,tái|儓,tái|鮐,tái|嬯,tái|擡,tái|薹,tái|籉,tái|抬,tái|呔,tǎi|忕,tài|太,tài|冭,tài|夳,tài|忲,tài|汰,tài|态,tài|肽,tài|钛,tài|泰,tài|粏,tài|舦,tài|酞,tài|鈦,tài|溙,tài|燤,tài|態,tài|坍,tān|贪,tān|怹,tān|啴,tān|痑,tān|舑,tān|貪,tān|摊,tān|滩,tān|嘽,tān|潬,tān|瘫,tān|擹,tān|攤,tān|灘,tān|癱,tān|镡,tán|蕁,tán|坛,tán|昙,tán|谈,tán|郯,tán|婒,tán|覃,tán|榃,tán|痰,tán|锬,tán|谭,tán|墵,tán|憛,tán|潭,tán|談,tán|壇,tán|曇,tán|錟,tán|檀,tán|顃,tán|罈,tán|藫,tán|壜,tán|譚,tán|貚,tán|醰,tán|譠,tán|罎,tán|鷤,tán|埮,tán|鐔,tán|墰,tán|忐,tǎn|坦,tǎn|袒,tǎn|钽,tǎn|菼,tǎn|毯,tǎn|鉭,tǎn|嗿,tǎn|憳,tǎn|憻,tǎn|醓,tǎn|璮,tǎn|襢,tǎn|緂,tǎn|暺,tǎn|叹,tàn|炭,tàn|探,tàn|湠,tàn|僋,tàn|嘆,tàn|碳,tàn|舕,tàn|歎,tàn|汤,tāng|铴,tāng|湯,tāng|嘡,tāng|劏,tāng|羰,tāng|蝪,tāng|薚,tāng|蹚,tāng|鐋,tāng|鞺,tāng|闛,tāng|耥,tāng|鼞,tāng|镗,táng|鏜,táng|饧,táng|坣,táng|唐,táng|堂,táng|傏,táng|啺,táng|棠,táng|鄌,táng|塘,táng|搪,táng|溏,táng|蓎,táng|隚,táng|榶,táng|漟,táng|煻,táng|瑭,táng|禟,táng|膅,táng|樘,táng|磄,táng|糃,táng|膛,táng|橖,táng|篖,táng|糖,táng|螗,táng|踼,táng|糛,táng|赯,táng|醣,táng|餳,táng|鎕,táng|餹,táng|饄,táng|鶶,táng|螳,táng|攩,tǎng|伖,tǎng|帑,tǎng|倘,tǎng|淌,tǎng|傥,tǎng|躺,tǎng|镋,tǎng|鎲,tǎng|儻,tǎng|戃,tǎng|曭,tǎng|爣,tǎng|矘,tǎng|钂,tǎng|烫,tàng|摥,tàng|趟,tàng|燙,tàng|漡,tàng|焘,tāo|轁,tāo|涭,tāo|仐,tāo|弢,tāo|绦,tāo|掏,tāo|絛,tāo|詜,tāo|嫍,tāo|幍,tāo|慆,tāo|搯,tāo|滔,tāo|槄,tāo|瑫,tāo|韬,tāo|飸,tāo|縚,tāo|縧,tāo|濤,tāo|謟,tāo|鞱,tāo|韜,tāo|饕,tāo|饀,tāo|燾,tāo|涛,tāo|迯,táo|咷,táo|洮,táo|逃,táo|桃,táo|陶,táo|啕,táo|梼,táo|淘,táo|萄,táo|祹,táo|裪,táo|綯,táo|蜪,táo|鞀,táo|醄,táo|鞉,táo|鋾,táo|駣,táo|檮,táo|騊,táo|鼗,táo|绹,táo|讨,tǎo|討,tǎo|套,tào|畓,tap|忑,tè|特,tè|貣,tè|脦,tè|犆,tè|铽,tè|慝,tè|鋱,tè|蟘,tè|螣,tè|鰧,teng|膯,tēng|鼟,tēng|疼,téng|痋,téng|幐,téng|腾,téng|誊,téng|漛,téng|滕,téng|邆,téng|縢,téng|駦,téng|謄,téng|儯,téng|藤,téng|騰,téng|籐,téng|籘,téng|虅,téng|驣,téng|霯,tèng|唞,teo|朰,teul|剔,tī|梯,tī|锑,tī|踢,tī|銻,tī|鷈,tī|鵜,tī|躰,tī|骵,tī|軆,tī|擿,tī|姼,tí|褆,tí|扌,tí|虒,tí|磃,tí|绨,tí|偍,tí|啼,tí|媞,tí|崹,tí|惿,tí|提,tí|稊,tí|缇,tí|罤,tí|遆,tí|鹈,tí|嗁,tí|瑅,tí|綈,tí|徲,tí|漽,tí|緹,tí|蕛,tí|蝭,tí|题,tí|趧,tí|蹄,tí|醍,tí|謕,tí|鍗,tí|題,tí|鮷,tí|騠,tí|鯷,tí|鶗,tí|鶙,tí|穉,tí|厗,tí|鳀,tí|徥,tǐ|体,tǐ|挮,tǐ|體,tǐ|衹,tǐ|戻,tì|屉,tì|剃,tì|洟,tì|倜,tì|悌,tì|涕,tì|逖,tì|屜,tì|悐,tì|惕,tì|掦,tì|逷,tì|惖,tì|替,tì|裼,tì|褅,tì|歒,tì|殢,tì|髰,tì|薙,tì|嚏,tì|鬀,tì|嚔,tì|瓋,tì|籊,tì|鐟,tì|楴,tì|天,tiān|兲,tiān|婖,tiān|添,tiān|酟,tiān|靔,tiān|黇,tiān|靝,tiān|呑,tiān|瞋,tián|田,tián|屇,tián|沺,tián|恬,tián|畋,tián|畑,tián|盷,tián|胋,tián|甛,tián|甜,tián|菾,tián|湉,tián|塡,tián|填,tián|搷,tián|阗,tián|碵,tián|磌,tián|窴,tián|鴫,tián|璳,tián|闐,tián|鷆,tián|鷏,tián|餂,tián|寘,tián|畠,tián|鍩,tiǎn|忝,tiǎn|殄,tiǎn|倎,tiǎn|唺,tiǎn|悿,tiǎn|捵,tiǎn|淟,tiǎn|晪,tiǎn|琠,tiǎn|腆,tiǎn|觍,tiǎn|睓,tiǎn|覥,tiǎn|賟,tiǎn|錪,tiǎn|娗,tiǎn|铦,tiǎn|銛,tiǎn|紾,tiǎn|舔,tiǎn|掭,tiàn|瑱,tiàn|睼,tiàn|舚,tiàn|旫,tiāo|佻,tiāo|庣,tiāo|挑,tiāo|祧,tiāo|聎,tiāo|苕,tiáo|萔,tiáo|芀,tiáo|条,tiáo|岧,tiáo|岹,tiáo|迢,tiáo|祒,tiáo|條,tiáo|笤,tiáo|蓚,tiáo|蓨,tiáo|龆,tiáo|樤,tiáo|蜩,tiáo|鋚,tiáo|髫,tiáo|鲦,tiáo|螩,tiáo|鯈,tiáo|鎥,tiáo|齠,tiáo|鰷,tiáo|趒,tiáo|銚,tiáo|儵,tiáo|鞗,tiáo|宨,tiǎo|晀,tiǎo|朓,tiǎo|脁,tiǎo|窕,tiǎo|窱,tiǎo|眺,tiào|粜,tiào|覜,tiào|跳,tiào|頫,tiào|糶,tiào|怗,tiē|贴,tiē|萜,tiē|聑,tiē|貼,tiē|帖,tiē|蛈,tiě|僣,tiě|鴩,tiě|鐵,tiě|驖,tiě|铁,tiě|呫,tiè|飻,tiè|餮,tiè|厅,tīng|庁,tīng|汀,tīng|听,tīng|耓,tīng|厛,tīng|烃,tīng|烴,tīng|綎,tīng|鞓,tīng|聴,tīng|聼,tīng|廰,tīng|聽,tīng|渟,tīng|廳,tīng|邒,tíng|廷,tíng|亭,tíng|庭,tíng|莛,tíng|停,tíng|婷,tíng|嵉,tíng|筳,tíng|葶,tíng|蜓,tíng|楟,tíng|榳,tíng|閮,tíng|霆,tíng|聤,tíng|蝏,tíng|諪,tíng|鼮,tíng|珵,tǐng|侱,tǐng|圢,tǐng|侹,tǐng|挺,tǐng|涏,tǐng|梃,tǐng|烶,tǐng|珽,tǐng|脡,tǐng|颋,tǐng|誔,tǐng|頲,tǐng|艇,tǐng|乭,tol|囲,tōng|炵,tōng|通,tōng|痌,tōng|嗵,tōng|蓪,tōng|樋,tōng|熥,tōng|爞,tóng|冂,tóng|燑,tóng|仝,tóng|同,tóng|佟,tóng|彤,tóng|峂,tóng|庝,tóng|哃,tóng|狪,tóng|茼,tóng|晍,tóng|桐,tóng|浵,tóng|砼,tóng|蚒,tóng|秱,tóng|铜,tóng|童,tóng|粡,tóng|赨,tóng|酮,tóng|鉖,tóng|僮,tóng|鉵,tóng|銅,tóng|餇,tóng|鲖,tóng|潼,tóng|獞,tóng|曈,tóng|朣,tóng|橦,tóng|氃,tóng|犝,tóng|膧,tóng|瞳,tóng|穜,tóng|鮦,tóng|眮,tóng|统,tǒng|捅,tǒng|桶,tǒng|筒,tǒng|綂,tǒng|統,tǒng|恸,tòng|痛,tòng|慟,tòng|憅,tòng|偷,tōu|偸,tōu|鍮,tōu|头,tóu|投,tóu|骰,tóu|緰,tóu|頭,tóu|钭,tǒu|妵,tǒu|紏,tǒu|敨,tǒu|斢,tǒu|黈,tǒu|蘣,tǒu|埱,tòu|透,tòu|綉,tòu|宊,tū|瑹,tū|凸,tū|禿,tū|秃,tū|突,tū|涋,tū|捸,tū|堗,tū|湥,tū|痜,tū|葖,tū|嶀,tū|鋵,tū|鵚,tū|鼵,tū|唋,tū|図,tú|图,tú|凃,tú|峹,tú|庩,tú|徒,tú|捈,tú|涂,tú|荼,tú|途,tú|屠,tú|梌,tú|揬,tú|稌,tú|塗,tú|嵞,tú|瘏,tú|筡,tú|鈯,tú|圖,tú|圗,tú|廜,tú|潳,tú|酴,tú|馟,tú|鍎,tú|駼,tú|鵌,tú|鶟,tú|鷋,tú|鷵,tú|兎,tú|菟,tú|蒤,tú|土,tǔ|圡,tǔ|吐,tǔ|汢,tǔ|钍,tǔ|釷,tǔ|迌,tù|兔,tù|莵,tù|堍,tù|鵵,tù|湍,tuān|猯,tuān|煓,tuān|蓴,tuán|团,tuán|団,tuán|抟,tuán|剸,tuán|團,tuán|塼,tuán|慱,tuán|摶,tuán|槫,tuán|漙,tuán|篿,tuán|檲,tuán|鏄,tuán|糰,tuán|鷒,tuán|鷻,tuán|嫥,tuán|鱄,tuán|圕,tuǎn|疃,tuǎn|畽,tuǎn|彖,tuàn|湪,tuàn|褖,tuàn|貒,tuàn|忒,tuī|推,tuī|蓷,tuī|藬,tuī|焞,tuī|騩,tuí|墤,tuí|颓,tuí|隤,tuí|尵,tuí|頹,tuí|頺,tuí|魋,tuí|穨,tuí|蘈,tuí|蹪,tuí|僓,tuí|頽,tuí|俀,tuǐ|脮,tuǐ|腿,tuǐ|蹆,tuǐ|骽,tuǐ|退,tuì|娧,tuì|煺,tuì|蛻,tuì|蜕,tuì|褪,tuì|駾,tuì|噋,tūn|汭,tūn|吞,tūn|旽,tūn|啍,tūn|朜,tūn|暾,tūn|黗,tūn|屯,tún|忳,tún|芚,tún|饨,tún|豚,tún|軘,tún|飩,tún|鲀,tún|魨,tún|霕,tún|臀,tún|臋,tún|坉,tún|豘,tún|氽,tǔn|舃,tuō|乇,tuō|讬,tuō|托,tuō|汑,tuō|饦,tuō|杔,tuō|侂,tuō|咃,tuō|拕,tuō|拖,tuō|侻,tuō|挩,tuō|捝,tuō|莌,tuō|袥,tuō|託,tuō|涶,tuō|脱,tuō|飥,tuō|馲,tuō|魠,tuō|驝,tuō|棁,tuō|脫,tuō|鱓,tuó|鋖,tuó|牠,tuó|驮,tuó|佗,tuó|陀,tuó|陁,tuó|坨,tuó|岮,tuó|沱,tuó|驼,tuó|柁,tuó|砣,tuó|砤,tuó|袉,tuó|鸵,tuó|紽,tuó|堶,tuó|跎,tuó|酡,tuó|碢,tuó|馱,tuó|槖,tuó|踻,tuó|駞,tuó|橐,tuó|鮀,tuó|鴕,tuó|鼧,tuó|騨,tuó|鼍,tuó|驒,tuó|鼉,tuó|迆,tuó|駝,tuó|軃,tuǒ|妥,tuǒ|毤,tuǒ|庹,tuǒ|椭,tuǒ|楕,tuǒ|鵎,tuǒ|拓,tuò|柝,tuò|唾,tuò|萚,tuò|跅,tuò|毻,tuò|箨,tuò|蘀,tuò|籜,tuò|哇,wa|窐,wā|劸,wā|徍,wā|挖,wā|洼,wā|娲,wā|畖,wā|窊,wā|媧,wā|嗗,wā|蛙,wā|搲,wā|溛,wā|漥,wā|窪,wā|鼃,wā|攨,wā|屲,wā|姽,wá|譁,wá|娃,wá|瓦,wǎ|佤,wǎ|邷,wǎ|咓,wǎ|瓲,wǎ|砙,wǎ|韎,wà|帓,wà|靺,wà|袜,wà|聉,wà|嗢,wà|腽,wà|膃,wà|韈,wà|韤,wà|襪,wà|咼,wāi|瀤,wāi|歪,wāi|喎,wāi|竵,wāi|崴,wǎi|外,wài|顡,wài|関,wān|闗,wān|夘,wān|乛,wān|弯,wān|剜,wān|婠,wān|帵,wān|塆,wān|湾,wān|睕,wān|蜿,wān|潫,wān|豌,wān|彎,wān|壪,wān|灣,wān|埦,wān|捥,wān|丸,wán|刓,wán|汍,wán|纨,wán|芄,wán|完,wán|岏,wán|忨,wán|玩,wán|笂,wán|紈,wán|捖,wán|顽,wán|烷,wán|琓,wán|貦,wán|頑,wán|蚖,wán|抏,wán|邜,wǎn|宛,wǎn|倇,wǎn|唍,wǎn|挽,wǎn|晚,wǎn|盌,wǎn|莞,wǎn|婉,wǎn|惋,wǎn|晩,wǎn|梚,wǎn|绾,wǎn|脘,wǎn|菀,wǎn|晼,wǎn|椀,wǎn|琬,wǎn|皖,wǎn|碗,wǎn|綩,wǎn|綰,wǎn|輓,wǎn|鋔,wǎn|鍐,wǎn|莬,wǎn|惌,wǎn|魭,wǎn|夗,wǎn|畹,wǎn|輐,wàn|鄤,wàn|孯,wàn|掔,wàn|万,wàn|卍,wàn|卐,wàn|妧,wàn|杤,wàn|腕,wàn|萬,wàn|翫,wàn|鋄,wàn|薍,wàn|錽,wàn|贃,wàn|鎫,wàn|贎,wàn|脕,wàn|尩,wāng|尪,wāng|尫,wāng|汪,wāng|瀇,wāng|亡,wáng|仼,wáng|彺,wáng|莣,wáng|蚟,wáng|王,wáng|抂,wǎng|网,wǎng|忹,wǎng|往,wǎng|徃,wǎng|枉,wǎng|罔,wǎng|惘,wǎng|菵,wǎng|暀,wǎng|棢,wǎng|焹,wǎng|蛧,wǎng|辋,wǎng|網,wǎng|蝄,wǎng|誷,wǎng|輞,wǎng|魍,wǎng|迬,wǎng|琞,wàng|妄,wàng|忘,wàng|迋,wàng|旺,wàng|盳,wàng|望,wàng|朢,wàng|威,wēi|烓,wēi|偎,wēi|逶,wēi|隇,wēi|隈,wēi|喴,wēi|媁,wēi|媙,wēi|愄,wēi|揋,wēi|揻,wēi|渨,wēi|煀,wēi|葨,wēi|葳,wēi|微,wēi|椳,wēi|楲,wēi|溦,wēi|煨,wēi|詴,wēi|縅,wēi|蝛,wēi|覣,wēi|嶶,wēi|薇,wēi|燰,wēi|鳂,wēi|癐,wēi|鰃,wēi|鰄,wēi|嵔,wēi|蜲,wēi|危,wēi|巍,wēi|恑,wéi|撝,wéi|囗,wéi|为,wéi|韦,wéi|围,wéi|帏,wéi|沩,wéi|违,wéi|闱,wéi|峗,wéi|峞,wéi|洈,wéi|為,wéi|韋,wéi|桅,wéi|涠,wéi|唯,wéi|帷,wéi|惟,wéi|维,wéi|喡,wéi|圍,wéi|嵬,wéi|幃,wéi|湋,wéi|溈,wéi|琟,wéi|潍,wéi|維,wéi|蓶,wéi|鄬,wéi|潿,wéi|醀,wéi|濰,wéi|鍏,wéi|闈,wéi|鮠,wéi|癓,wéi|覹,wéi|犩,wéi|霺,wéi|僞,wéi|寪,wéi|觹,wéi|觽,wéi|觿,wéi|欈,wéi|違,wéi|趡,wěi|磈,wěi|瓗,wěi|膸,wěi|撱,wěi|鰖,wěi|伟,wěi|伪,wěi|尾,wěi|纬,wěi|芛,wěi|苇,wěi|委,wěi|炜,wěi|玮,wěi|洧,wěi|娓,wěi|捤,wěi|浘,wěi|诿,wěi|偉,wěi|偽,wěi|崣,wěi|梶,wěi|硊,wěi|萎,wěi|隗,wěi|骩,wěi|廆,wěi|徫,wěi|愇,wěi|猥,wěi|葦,wěi|蒍,wěi|骪,wěi|骫,wěi|暐,wěi|椲,wěi|煒,wěi|瑋,wěi|痿,wěi|腲,wěi|艉,wěi|韪,wěi|碨,wěi|鲔,wěi|緯,wěi|蔿,wěi|諉,wěi|踓,wěi|韑,wěi|頠,wěi|薳,wěi|儰,wěi|濻,wěi|鍡,wěi|鮪,wěi|壝,wěi|韙,wěi|颹,wěi|瀢,wěi|韡,wěi|亹,wěi|斖,wěi|茟,wěi|蜹,wèi|爲,wèi|卫,wèi|未,wèi|位,wèi|味,wèi|苿,wèi|畏,wèi|胃,wèi|叞,wèi|軎,wèi|尉,wèi|菋,wèi|谓,wèi|喂,wèi|媦,wèi|渭,wèi|猬,wèi|煟,wèi|墛,wèi|蔚,wèi|慰,wèi|熭,wèi|犚,wèi|磑,wèi|緭,wèi|蝟,wèi|衛,wèi|懀,wèi|濊,wèi|璏,wèi|罻,wèi|衞,wèi|謂,wèi|錗,wèi|餧,wèi|鮇,wèi|螱,wèi|褽,wèi|餵,wèi|魏,wèi|藯,wèi|鏏,wèi|霨,wèi|鳚,wèi|蘶,wèi|饖,wèi|讆,wèi|躗,wèi|讏,wèi|躛,wèi|荱,wèi|蜼,wèi|硙,wèi|轊,wèi|昷,wēn|塭,wēn|温,wēn|榅,wēn|殟,wēn|溫,wēn|瑥,wēn|辒,wēn|榲,wēn|瘟,wēn|豱,wēn|輼,wēn|鳁,wēn|鎾,wēn|饂,wēn|鰛,wēn|鰮,wēn|褞,wēn|缊,wēn|緼,wēn|蕰,wēn|縕,wēn|薀,wēn|藴,wēn|鴖,wén|亠,wén|文,wén|彣,wén|纹,wén|炆,wén|砇,wén|闻,wén|紋,wén|蚉,wén|蚊,wén|珳,wén|阌,wén|鈫,wén|雯,wén|瘒,wén|聞,wén|馼,wén|魰,wén|鳼,wén|鴍,wén|螡,wén|閺,wén|閿,wén|蟁,wén|闅,wén|鼤,wén|闦,wén|芠,wén|呅,wěn|忞,wěn|歾,wěn|刎,wěn|吻,wěn|呚,wěn|忟,wěn|抆,wěn|呡,wěn|紊,wěn|桽,wěn|脗,wěn|稳,wěn|穏,wěn|穩,wěn|肳,wěn|问,wèn|妏,wèn|汶,wèn|問,wèn|渂,wèn|搵,wèn|絻,wèn|顐,wèn|璺,wèn|翁,wēng|嗡,wēng|鹟,wēng|螉,wēng|鎓,wēng|鶲,wēng|滃,wēng|奣,wěng|塕,wěng|嵡,wěng|蓊,wěng|瞈,wěng|聬,wěng|暡,wěng|瓮,wèng|蕹,wèng|甕,wèng|罋,wèng|齆,wèng|堝,wō|濄,wō|薶,wō|捼,wō|挝,wō|倭,wō|涡,wō|莴,wō|唩,wō|涹,wō|渦,wō|猧,wō|萵,wō|喔,wō|窝,wō|窩,wō|蜗,wō|撾,wō|蝸,wō|踒,wō|涴,wó|我,wǒ|婐,wǒ|婑,wǒ|捰,wǒ|龏,wò|蒦,wò|嚄,wò|雘,wò|艧,wò|踠,wò|仴,wò|沃,wò|肟,wò|臥,wò|偓,wò|捾,wò|媉,wò|幄,wò|握,wò|渥,wò|硪,wò|楃,wò|腛,wò|斡,wò|瞃,wò|濣,wò|瓁,wò|龌,wò|齷,wò|枂,wò|馧,wò|卧,wò|扝,wū|乌,wū|圬,wū|弙,wū|污,wū|邬,wū|呜,wū|杇,wū|巫,wū|屋,wū|洿,wū|钨,wū|烏,wū|趶,wū|剭,wū|窏,wū|釫,wū|鄔,wū|嗚,wū|誈,wū|誣,wū|箼,wū|螐,wū|鴮,wū|鎢,wū|鰞,wū|兀,wū|杅,wū|诬,wū|幠,wú|譕,wú|蟱,wú|墲,wú|亾,wú|兦,wú|无,wú|毋,wú|吳,wú|吴,wú|吾,wú|呉,wú|芜,wú|郚,wú|娪,wú|梧,wú|洖,wú|浯,wú|茣,wú|珸,wú|祦,wú|鹀,wú|無,wú|禑,wú|蜈,wú|蕪,wú|璑,wú|鵐,wú|鯃,wú|鼯,wú|鷡,wú|俉,wú|憮,wú|橆,wú|铻,wú|鋙,wú|莁,wú|陚,wǔ|瞴,wǔ|娒,wǔ|乄,wǔ|五,wǔ|午,wǔ|仵,wǔ|伍,wǔ|妩,wǔ|庑,wǔ|忤,wǔ|怃,wǔ|迕,wǔ|旿,wǔ|武,wǔ|玝,wǔ|侮,wǔ|倵,wǔ|捂,wǔ|娬,wǔ|牾,wǔ|珷,wǔ|摀,wǔ|熓,wǔ|碔,wǔ|鹉,wǔ|瑦,wǔ|舞,wǔ|嫵,wǔ|廡,wǔ|潕,wǔ|錻,wǔ|儛,wǔ|甒,wǔ|鵡,wǔ|躌,wǔ|逜,wǔ|膴,wǔ|啎,wǔ|噁,wù|雺,wù|渞,wù|揾,wù|坞,wù|塢,wù|勿,wù|务,wù|戊,wù|阢,wù|伆,wù|屼,wù|扤,wù|岉,wù|杌,wù|忢,wù|物,wù|矹,wù|敄,wù|误,wù|務,wù|悞,wù|悟,wù|悮,wù|粅,wù|晤,wù|焐,wù|婺,wù|嵍,wù|痦,wù|隖,wù|靰,wù|骛,wù|奦,wù|嵨,wù|溩,wù|雾,wù|寤,wù|熃,wù|誤,wù|鹜,wù|鋈,wù|窹,wù|鼿,wù|霧,wù|齀,wù|騖,wù|鶩,wù|芴,wù|霚,wù|扱,xī|糦,xī|宩,xī|獡,xī|蜤,xī|燍,xī|夕,xī|兮,xī|汐,xī|西,xī|覀,xī|吸,xī|希,xī|扸,xī|卥,xī|昔,xī|析,xī|矽,xī|穸,xī|肹,xī|俙,xī|徆,xī|怸,xī|郗,xī|饻,xī|唏,xī|奚,xī|屖,xī|息,xī|悕,xī|晞,xī|氥,xī|浠,xī|牺,xī|狶,xī|莃,xī|唽,xī|悉,xī|惜,xī|桸,xī|欷,xī|淅,xī|渓,xī|烯,xī|焁,xī|焈,xī|琋,xī|硒,xī|菥,xī|赥,xī|釸,xī|傒,xī|惁,xī|晰,xī|晳,xī|焟,xī|犀,xī|睎,xī|稀,xī|粞,xī|翕,xī|翖,xī|舾,xī|鄎,xī|厀,xī|嵠,xī|徯,xī|溪,xī|煕,xī|皙,xī|蒠,xī|锡,xī|僖,xī|榽,xī|熄,xī|熙,xī|緆,xī|蜥,xī|豨,xī|餏,xī|嘻,xī|噏,xī|嬆,xī|嬉,xī|膝,xī|餙,xī|凞,xī|樨,xī|橀,xī|歙,xī|熹,xī|熺,xī|熻,xī|窸,xī|羲,xī|螅,xī|錫,xī|燨,xī|犠,xī|瞦,xī|礂,xī|蟋,xī|豀,xī|豯,xī|貕,xī|繥,xī|鯑,xī|鵗,xī|譆,xī|鏭,xī|隵,xī|巇,xī|曦,xī|爔,xī|犧,xī|酅,xī|鼷,xī|蠵,xī|鸂,xī|鑴,xī|憘,xī|暿,xī|鱚,xī|咥,xī|訢,xī|娭,xī|瘜,xī|醯,xī|雭,xí|习,xí|郋,xí|席,xí|習,xí|袭,xí|觋,xí|媳,xí|椺,xí|蒵,xí|蓆,xí|嶍,xí|漝,xí|覡,xí|趘,xí|薂,xí|檄,xí|謵,xí|鎴,xí|霫,xí|鳛,xí|飁,xí|騱,xí|騽,xí|襲,xí|鰼,xí|驨,xí|隰,xí|囍,xǐ|杫,xǐ|枲,xǐ|洗,xǐ|玺,xǐ|徙,xǐ|铣,xǐ|喜,xǐ|葈,xǐ|葸,xǐ|鈢,xǐ|屣,xǐ|漇,xǐ|蓰,xǐ|銑,xǐ|憙,xǐ|橲,xǐ|禧,xǐ|諰,xǐ|壐,xǐ|縰,xǐ|謑,xǐ|蟢,xǐ|蹝,xǐ|璽,xǐ|躧,xǐ|鉩,xǐ|欪,xì|钑,xì|鈒,xì|匸,xì|卌,xì|戏,xì|屃,xì|系,xì|饩,xì|呬,xì|忥,xì|怬,xì|细,xì|係,xì|恄,xì|绤,xì|釳,xì|阋,xì|塈,xì|椞,xì|舄,xì|趇,xì|隙,xì|慀,xì|滊,xì|禊,xì|綌,xì|赩,xì|隟,xì|熂,xì|犔,xì|潟,xì|澙,xì|蕮,xì|覤,xì|黖,xì|戲,xì|磶,xì|虩,xì|餼,xì|鬩,xì|嚱,xì|霼,xì|衋,xì|細,xì|闟,xì|虾,xiā|谺,xiā|傄,xiā|閕,xiā|敮,xiā|颬,xiā|瞎,xiā|蝦,xiā|鰕,xiā|魻,xiā|郃,xiá|匣,xiá|侠,xiá|狎,xiá|俠,xiá|峡,xiá|柙,xiá|炠,xiá|狭,xiá|陜,xiá|峽,xiá|烚,xiá|狹,xiá|珨,xiá|祫,xiá|硖,xiá|舺,xiá|陿,xiá|溊,xiá|硤,xiá|遐,xiá|暇,xiá|瑕,xiá|筪,xiá|碬,xiá|舝,xiá|辖,xiá|縀,xiá|蕸,xiá|縖,xiá|赮,xiá|轄,xiá|鍜,xiá|霞,xiá|鎋,xiá|黠,xiá|騢,xiá|鶷,xiá|睱,xiá|翈,xiá|昰,xià|丅,xià|下,xià|吓,xià|圷,xià|夏,xià|梺,xià|嚇,xià|懗,xià|罅,xià|鏬,xià|疜,xià|姺,xiān|仙,xiān|仚,xiān|屳,xiān|先,xiān|奾,xiān|纤,xiān|佡,xiān|忺,xiān|氙,xiān|祆,xiān|秈,xiān|苮,xiān|枮,xiān|籼,xiān|珗,xiān|莶,xiān|掀,xiān|酰,xiān|锨,xiān|僊,xiān|僲,xiān|嘕,xiān|鲜,xiān|暹,xiān|韯,xiān|憸,xiān|鍁,xiān|繊,xiān|褼,xiān|韱,xiān|鮮,xiān|馦,xiān|蹮,xiān|廯,xiān|譣,xiān|鶱,xiān|襳,xiān|躚,xiān|纖,xiān|鱻,xiān|縿,xiān|跹,xiān|咞,xián|闲,xián|妶,xián|弦,xián|贤,xián|咸,xián|挦,xián|涎,xián|胘,xián|娴,xián|娹,xián|婱,xián|舷,xián|蚿,xián|衔,xián|啣,xián|痫,xián|蛝,xián|閑,xián|鹇,xián|嫌,xián|甉,xián|銜,xián|嫺,xián|嫻,xián|憪,xián|澖,xián|誸,xián|賢,xián|癇,xián|癎,xián|礥,xián|贒,xián|鑦,xián|鷳,xián|鷴,xián|鷼,xián|伭,xián|冼,xiǎn|狝,xiǎn|显,xiǎn|险,xiǎn|毨,xiǎn|烍,xiǎn|猃,xiǎn|蚬,xiǎn|険,xiǎn|赻,xiǎn|筅,xiǎn|尟,xiǎn|尠,xiǎn|禒,xiǎn|蜆,xiǎn|跣,xiǎn|箲,xiǎn|險,xiǎn|獫,xiǎn|獮,xiǎn|藓,xiǎn|鍌,xiǎn|燹,xiǎn|顕,xiǎn|幰,xiǎn|攇,xiǎn|櫶,xiǎn|蘚,xiǎn|玁,xiǎn|韅,xiǎn|顯,xiǎn|灦,xiǎn|搟,xiǎn|县,xiàn|岘,xiàn|苋,xiàn|现,xiàn|线,xiàn|臽,xiàn|限,xiàn|姭,xiàn|宪,xiàn|陥,xiàn|哯,xiàn|垷,xiàn|娨,xiàn|峴,xiàn|晛,xiàn|莧,xiàn|陷,xiàn|現,xiàn|馅,xiàn|睍,xiàn|絤,xiàn|缐,xiàn|羡,xiàn|献,xiàn|粯,xiàn|羨,xiàn|腺,xiàn|僩,xiàn|僴,xiàn|綫,xiàn|誢,xiàn|撊,xiàn|線,xiàn|鋧,xiàn|憲,xiàn|餡,xiàn|豏,xiàn|瀗,xiàn|臔,xiàn|獻,xiàn|鏾,xiàn|霰,xiàn|鼸,xiàn|脇,xiàn|軐,xiàn|県,xiàn|縣,xiàn|儴,xiāng|勷,xiāng|蘘,xiāng|纕,xiāng|乡,xiāng|芗,xiāng|香,xiāng|郷,xiāng|厢,xiāng|鄉,xiāng|鄊,xiāng|廂,xiāng|湘,xiāng|缃,xiāng|葙,xiāng|鄕,xiāng|楿,xiāng|薌,xiāng|箱,xiāng|緗,xiāng|膷,xiāng|忀,xiāng|骧,xiāng|麘,xiāng|欀,xiāng|瓖,xiāng|镶,xiāng|鱜,xiāng|鑲,xiāng|驤,xiāng|襄,xiāng|佭,xiáng|详,xiáng|庠,xiáng|栙,xiáng|祥,xiáng|絴,xiáng|翔,xiáng|詳,xiáng|跭,xiáng|享,xiǎng|亯,xiǎng|响,xiǎng|蚃,xiǎng|饷,xiǎng|晑,xiǎng|飨,xiǎng|想,xiǎng|餉,xiǎng|鲞,xiǎng|蠁,xiǎng|鮝,xiǎng|鯗,xiǎng|響,xiǎng|饗,xiǎng|饟,xiǎng|鱶,xiǎng|傢,xiàng|相,xiàng|向,xiàng|姠,xiàng|巷,xiàng|项,xiàng|珦,xiàng|象,xiàng|缿,xiàng|萫,xiàng|項,xiàng|像,xiàng|勨,xiàng|嶑,xiàng|橡,xiàng|襐,xiàng|蟓,xiàng|鐌,xiàng|鱌,xiàng|鋞,xiàng|鬨,xiàng|嚮,xiàng|鵁,xiāo|莦,xiāo|颵,xiāo|箾,xiāo|潚,xiāo|橚,xiāo|灱,xiāo|灲,xiāo|枭,xiāo|侾,xiāo|哓,xiāo|枵,xiāo|骁,xiāo|宯,xiāo|宵,xiāo|庨,xiāo|恷,xiāo|消,xiāo|绡,xiāo|虓,xiāo|逍,xiāo|鸮,xiāo|啋,xiāo|婋,xiāo|梟,xiāo|焇,xiāo|猇,xiāo|萧,xiāo|痚,xiāo|痟,xiāo|硝,xiāo|硣,xiāo|窙,xiāo|翛,xiāo|萷,xiāo|销,xiāo|揱,xiāo|綃,xiāo|歊,xiāo|箫,xiāo|嘵,xiāo|撨,xiāo|獢,xiāo|銷,xiāo|霄,xiāo|彇,xiāo|膮,xiāo|蕭,xiāo|魈,xiāo|鴞,xiāo|穘,xiāo|簘,xiāo|蟂,xiāo|蟏,xiāo|鴵,xiāo|嚣,xiāo|瀟,xiāo|簫,xiāo|蟰,xiāo|髇,xiāo|囂,xiāo|髐,xiāo|鷍,xiāo|驍,xiāo|毊,xiāo|虈,xiāo|肖,xiāo|哮,xiāo|烋,xiāo|潇,xiāo|蠨,xiāo|洨,xiáo|崤,xiáo|淆,xiáo|誵,xiáo|笹,xiǎo|小,xiǎo|晓,xiǎo|暁,xiǎo|筱,xiǎo|筿,xiǎo|曉,xiǎo|篠,xiǎo|謏,xiǎo|皢,xiǎo|孝,xiào|効,xiào|咲,xiào|俲,xiào|效,xiào|校,xiào|涍,xiào|笑,xiào|傚,xiào|敩,xiào|滧,xiào|詨,xiào|嘋,xiào|嘨,xiào|誟,xiào|嘯,xiào|熽,xiào|斅,xiào|斆,xiào|澩,xiào|啸,xiào|些,xiē|楔,xiē|歇,xiē|蝎,xiē|蠍,xiē|协,xié|旪,xié|邪,xié|協,xié|胁,xié|垥,xié|恊,xié|拹,xié|脋,xié|衺,xié|偕,xié|斜,xié|谐,xié|翓,xié|嗋,xié|愶,xié|携,xié|瑎,xié|綊,xié|熁,xié|膎,xié|勰,xié|撷,xié|擕,xié|緳,xié|缬,xié|蝢,xié|鞋,xié|諧,xié|燲,xié|擷,xié|鞵,xié|襭,xié|攜,xié|讗,xié|龤,xié|魼,xié|脅,xié|纈,xié|写,xiě|冩,xiě|寫,xiě|藛,xiě|烲,xiè|榝,xiè|齛,xiè|碿,xiè|伳,xiè|灺,xiè|泄,xiè|泻,xiè|祄,xiè|绁,xiè|缷,xiè|卸,xiè|洩,xiè|炧,xiè|炨,xiè|卨,xiè|娎,xiè|屑,xiè|屓,xiè|偰,xiè|徢,xiè|械,xiè|焎,xiè|禼,xiè|亵,xiè|媟,xiè|屟,xiè|渫,xiè|絬,xiè|谢,xiè|僁,xiè|塮,xiè|榍,xiè|榭,xiè|褉,xiè|噧,xiè|屧,xiè|暬,xiè|韰,xiè|廨,xiè|懈,xiè|澥,xiè|獬,xiè|糏,xiè|薢,xiè|薤,xiè|邂,xiè|燮,xiè|褻,xiè|謝,xiè|夑,xiè|瀉,xiè|鞢,xiè|瀣,xiè|蟹,xiè|蠏,xiè|齘,xiè|齥,xiè|齂,xiè|躠,xiè|屭,xiè|躞,xiè|蝑,xiè|揳,xiè|爕,xiè|噺,xin|心,xīn|邤,xīn|妡,xīn|忻,xīn|芯,xīn|辛,xīn|昕,xīn|杺,xīn|欣,xīn|盺,xīn|俽,xīn|惞,xīn|锌,xīn|新,xīn|歆,xīn|鋅,xīn|嬜,xīn|薪,xīn|馨,xīn|鑫,xīn|馫,xīn|枔,xín|襑,xín|潃,xǐn|阠,xìn|伩,xìn|囟,xìn|孞,xìn|炘,xìn|信,xìn|脪,xìn|衅,xìn|訫,xìn|焮,xìn|舋,xìn|釁,xìn|狌,xīng|星,xīng|垶,xīng|骍,xīng|猩,xīng|煋,xīng|鷞,shuāng|骦,shuāng|縔,shuǎng|艭,shuāng|塽,shuǎng|壯,zhuàng|状,zhuàng|狀,zhuàng|壵,zhuàng|梉,zhuàng|瑆,xīng|腥,xīng|蛵,xīng|觪,xīng|箵,xīng|篂,xīng|謃,xīng|鮏,xīng|曐,xīng|觲,xīng|騂,xīng|皨,xīng|鯹,xīng|嬹,xīng|惺,xīng|刑,xíng|邢,xíng|形,xíng|陉,xíng|侀,xíng|哘,xíng|型,xíng|洐,xíng|娙,xíng|硎,xíng|铏,xíng|鉶,xíng|裄,xíng|睲,xǐng|醒,xǐng|擤,xǐng|兴,xìng|興,xìng|杏,xìng|姓,xìng|幸,xìng|性,xìng|荇,xìng|倖,xìng|莕,xìng|婞,xìng|悻,xìng|涬,xìng|緈,xìng|臖,xìng|凶,xiōng|兄,xiōng|兇,xiōng|匈,xiōng|芎,xiōng|讻,xiōng|忷,xiōng|汹,xiōng|恟,xiōng|洶,xiōng|胷,xiōng|胸,xiōng|訩,xiōng|詾,xiōng|哅,xiōng|雄,xióng|熊,xióng|诇,xiòng|詗,xiòng|敻,xiòng|休,xiū|俢,xiū|修,xiū|咻,xiū|庥,xiū|烌,xiū|羞,xiū|脙,xiū|鸺,xiū|臹,xiū|貅,xiū|馐,xiū|樇,xiū|銝,xiū|髤,xiū|髹,xiū|鮴,xiū|鵂,xiū|饈,xiū|鏅,xiū|飍,xiū|鎀,xiū|苬,xiú|宿,xiǔ|朽,xiǔ|綇,xiǔ|滫,xiǔ|糔,xiǔ|臰,xiù|秀,xiù|岫,xiù|珛,xiù|绣,xiù|袖,xiù|琇,xiù|锈,xiù|溴,xiù|璓,xiù|螑,xiù|繍,xiù|繡,xiù|鏥,xiù|鏽,xiù|齅,xiù|嗅,xiù|蓿,xu|繻,xū|圩,xū|旴,xū|疞,xū|盱,xū|欨,xū|胥,xū|须,xū|顼,xū|虗,xū|虚,xū|谞,xū|媭,xū|幁,xū|欻,xū|虛,xū|須,xū|楈,xū|窢,xū|頊,xū|嘘,xū|稰,xū|需,xū|魆,xū|噓,xū|墟,xū|嬃,xū|歔,xū|縃,xū|歘,xū|諝,xū|譃,xū|魖,xū|驉,xū|鑐,xū|鬚,xū|姁,xū|偦,xū|戌,xū|蕦,xū|俆,xú|徐,xú|蒣,xú|訏,xǔ|许,xǔ|诩,xǔ|冔,xǔ|栩,xǔ|珝,xǔ|許,xǔ|湑,xǔ|暊,xǔ|詡,xǔ|鄦,xǔ|糈,xǔ|醑,xǔ|盨,xǔ|滀,xù|嘼,xù|鉥,xù|旭,xù|伵,xù|序,xù|侐,xù|沀,xù|叙,xù|恤,xù|昫,xù|洫,xù|垿,xù|欰,xù|殈,xù|烅,xù|珬,xù|勖,xù|勗,xù|敍,xù|敘,xù|烼,xù|绪,xù|续,xù|酗,xù|喣,xù|壻,xù|婿,xù|朂,xù|溆,xù|絮,xù|訹,xù|慉,xù|続,xù|蓄,xù|賉,xù|槒,xù|漵,xù|潊,xù|盢,xù|瞁,xù|緒,xù|聟,xù|稸,xù|緖,xù|瞲,xù|藚,xù|續,xù|怴,xù|芧,xù|汿,xù|煦,xù|煖,xuān|吅,xuān|轩,xuān|昍,xuān|咺,xuān|宣,xuān|晅,xuān|軒,xuān|谖,xuān|喧,xuān|媗,xuān|愃,xuān|愋,xuān|揎,xuān|萱,xuān|萲,xuān|暄,xuān|煊,xuān|瑄,xuān|蓒,xuān|睻,xuān|儇,xuān|禤,xuān|箮,xuān|翧,xuān|蝖,xuān|蕿,xuān|諠,xuān|諼,xuān|鍹,xuān|駽,xuān|矎,xuān|翾,xuān|藼,xuān|蘐,xuān|蠉,xuān|譞,xuān|鰚,xuān|塇,xuān|玹,xuán|痃,xuán|悬,xuán|旋,xuán|蜁,xuán|嫙,xuán|漩,xuán|暶,xuán|璇,xuán|檈,xuán|璿,xuán|懸,xuán|玆,xuán|玄,xuán|选,xuǎn|選,xuǎn|癣,xuǎn|癬,xuǎn|絃,xuàn|夐,xuàn|怰,xuàn|泫,xuàn|昡,xuàn|炫,xuàn|绚,xuàn|眩,xuàn|袨,xuàn|铉,xuàn|琄,xuàn|眴,xuàn|衒,xuàn|絢,xuàn|楦,xuàn|鉉,xuàn|碹,xuàn|蔙,xuàn|镟,xuàn|颴,xuàn|縼,xuàn|繏,xuàn|鏇,xuàn|贙,xuàn|駨,xuàn|渲,xuàn|疶,xuē|蒆,xuē|靴,xuē|薛,xuē|鞾,xuē|削,xuē|噱,xué|穴,xué|斈,xué|乴,xué|坹,xué|学,xué|岤,xué|峃,xué|茓,xué|泶,xué|袕,xué|鸴,xué|學,xué|嶨,xué|燢,xué|雤,xué|鷽,xué|踅,xué|雪,xuě|樰,xuě|膤,xuě|艝,xuě|轌,xuě|鳕,xuě|鱈,xuě|血,xuè|泧,xuè|狘,xuè|桖,xuè|烕,xuè|谑,xuè|趐,xuè|瀥,xuè|坃,xūn|勋,xūn|埙,xūn|塤,xūn|熏,xūn|窨,xūn|勲,xūn|勳,xūn|薫,xūn|嚑,xūn|壎,xūn|獯,xūn|薰,xūn|曛,xūn|燻,xūn|臐,xūn|矄,xūn|蘍,xūn|壦,xūn|爋,xūn|纁,xūn|醺,xūn|勛,xūn|郇,xún|咰,xún|寻,xún|巡,xún|旬,xún|杊,xún|询,xún|峋,xún|恂,xún|浔,xún|紃,xún|荀,xún|栒,xún|桪,xún|毥,xún|珣,xún|偱,xún|尋,xún|循,xún|揗,xún|詢,xún|鄩,xún|鲟,xún|噚,xún|潯,xún|攳,xún|樳,xún|燅,xún|燖,xún|璕,xún|蟳,xún|鱏,xún|鱘,xún|侚,xún|彐,xún|撏,xún|洵,xún|浚,xùn|濬,xùn|鶽,xùn|驯,xùn|馴,xùn|卂,xùn|训,xùn|伨,xùn|汛,xùn|迅,xùn|徇,xùn|狥,xùn|迿,xùn|逊,xùn|殉,xùn|訊,xùn|訓,xùn|訙,xùn|奞,xùn|巽,xùn|殾,xùn|遜,xùn|愻,xùn|賐,xùn|噀,xùn|蕈,xùn|顨,xùn|鑂,xùn|稄,xùn|讯,xùn|呀,ya|圧,yā|丫,yā|压,yā|庘,yā|押,yā|鸦,yā|桠,yā|鸭,yā|铔,yā|椏,yā|鴉,yā|錏,yā|鴨,yā|壓,yā|鵶,yā|鐚,yā|唖,yā|亜,yā|垭,yā|俹,yā|埡,yā|孲,yā|拁,yá|疨,yá|牙,yá|伢,yá|岈,yá|芽,yá|厓,yá|枒,yá|琊,yá|笌,yá|蚜,yá|堐,yá|崕,yá|崖,yá|涯,yá|猚,yá|瑘,yá|睚,yá|衙,yá|漄,yá|齖,yá|庌,yá|顔,yá|釾,yá|疋,yǎ|厊,yǎ|啞,yǎ|痖,yǎ|雅,yǎ|瘂,yǎ|蕥,yǎ|挜,yǎ|掗,yǎ|哑,yǎ|呾,yà|輵,yà|潝,yà|劜,yà|圠,yà|亚,yà|穵,yà|襾,yà|讶,yà|犽,yà|迓,yà|亞,yà|玡,yà|娅,yà|砑,yà|氩,yà|婭,yà|訝,yà|揠,yà|氬,yà|猰,yà|圔,yà|稏,yà|窫,yà|椻,yà|鼼,yà|聐,yà|淊,yān|咽,yān|恹,yān|剦,yān|烟,yān|珚,yān|胭,yān|偣,yān|崦,yān|淹,yān|焉,yān|菸,yān|阉,yān|湮,yān|腌,yān|傿,yān|煙,yān|鄢,yān|嫣,yān|漹,yān|嶖,yān|樮,yān|醃,yān|閹,yān|嬮,yān|篶,yān|臙,yān|黫,yān|弇,yān|硽,yān|慇,yān|黰,yān|橪,yān|阽,yán|炏,yán|挻,yán|厃,yán|唌,yán|廵,yán|讠,yán|円,yán|延,yán|闫,yán|严,yán|妍,yán|言,yán|訁,yán|岩,yán|昖,yán|沿,yán|炎,yán|郔,yán|姸,yán|娫,yán|狿,yán|研,yán|莚,yán|娮,yán|盐,yán|琂,yán|硏,yán|訮,yán|閆,yán|阎,yán|嵒,yán|嵓,yán|綖,yán|蜒,yán|塩,yán|揅,yán|楌,yán|詽,yán|碞,yán|蔅,yán|颜,yán|虤,yán|閻,yán|厳,yán|檐,yán|顏,yán|嚴,yán|壛,yán|巌,yán|簷,yán|櫩,yán|麙,yán|壧,yán|孍,yán|巖,yán|巗,yán|巚,yán|欕,yán|礹,yán|鹽,yán|麣,yán|黬,yán|偐,yán|贗,yán|菴,yǎn|剡,yǎn|嬐,yǎn|崄,yǎn|嶮,yǎn|抁,yǎn|沇,yǎn|乵,yǎn|兖,yǎn|奄,yǎn|俨,yǎn|兗,yǎn|匽,yǎn|衍,yǎn|偃,yǎn|厣,yǎn|掩,yǎn|眼,yǎn|萒,yǎn|郾,yǎn|酓,yǎn|嵃,yǎn|愝,yǎn|扊,yǎn|揜,yǎn|棪,yǎn|渰,yǎn|渷,yǎn|琰,yǎn|隒,yǎn|椼,yǎn|罨,yǎn|演,yǎn|褗,yǎn|蝘,yǎn|魇,yǎn|噞,yǎn|躽,yǎn|檿,yǎn|黡,yǎn|厴,yǎn|甗,yǎn|鰋,yǎn|鶠,yǎn|黤,yǎn|齞,yǎn|儼,yǎn|黭,yǎn|顩,yǎn|鼴,yǎn|巘,yǎn|曮,yǎn|魘,yǎn|鼹,yǎn|齴,yǎn|黶,yǎn|掞,yǎn|隁,yǎn|喭,yǎn|酀,yǎn|龂,yǎn|齗,yǎn|阭,yǎn|夵,yǎn|裺,yǎn|溎,yàn|豜,yàn|豣,yàn|烻,yàn|湺,yàn|麲,yàn|厌,yàn|妟,yàn|牪,yàn|姲,yàn|彥,yàn|彦,yàn|砚,yàn|唁,yàn|宴,yàn|晏,yàn|艳,yàn|覎,yàn|验,yàn|焔,yàn|谚,yàn|堰,yàn|敥,yàn|焰,yàn|焱,yàn|猒,yàn|硯,yàn|葕,yàn|雁,yàn|滟,yàn|鳫,yàn|厭,yàn|墕,yàn|熖,yàn|酽,yàn|嬊,yàn|谳,yàn|餍,yàn|鴈,yàn|燄,yàn|燕,yàn|諺,yàn|赝,yàn|鬳,yàn|曕,yàn|騐,yàn|験,yàn|嚥,yàn|嬿,yàn|艶,yàn|贋,yàn|軅,yàn|爓,yàn|醶,yàn|騴,yàn|鷃,yàn|灔,yàn|觾,yàn|讌,yàn|饜,yàn|驗,yàn|鷰,yàn|艷,yàn|灎,yàn|釅,yàn|驠,yàn|灧,yàn|讞,yàn|豓,yàn|豔,yàn|灩,yàn|顑,yàn|懕,yàn|筵,yàn|觃,yàn|暥,yàn|醼,yàn|歍,yāng|央,yāng|咉,yāng|姎,yāng|抰,yāng|泱,yāng|殃,yāng|胦,yāng|眏,yāng|秧,yāng|鸯,yāng|鉠,yāng|雵,yāng|鞅,yāng|鍈,yāng|鴦,yāng|佒,yāng|霙,yāng|瑒,yáng|婸,yáng|扬,yáng|羊,yáng|阦,yáng|旸,yáng|杨,yáng|炀,yáng|佯,yáng|劷,yáng|氜,yáng|疡,yáng|钖,yáng|飏,yáng|垟,yáng|徉,yáng|昜,yáng|洋,yáng|羏,yáng|烊,yáng|珜,yáng|眻,yáng|陽,yáng|崵,yáng|崸,yáng|揚,yáng|蛘,yáng|敭,yáng|暘,yáng|楊,yáng|煬,yáng|禓,yáng|瘍,yáng|諹,yáng|輰,yáng|鴹,yáng|颺,yáng|鐊,yáng|鰑,yáng|霷,yáng|鸉,yáng|阳,yáng|鍚,yáng|飬,yǎng|勜,yǎng|仰,yǎng|坱,yǎng|奍,yǎng|岟,yǎng|养,yǎng|炴,yǎng|氧,yǎng|痒,yǎng|紻,yǎng|傟,yǎng|楧,yǎng|軮,yǎng|慃,yǎng|氱,yǎng|羪,yǎng|養,yǎng|駚,yǎng|懩,yǎng|攁,yǎng|瀁,yǎng|癢,yǎng|礢,yǎng|柍,yǎng|恙,yàng|样,yàng|羕,yàng|詇,yàng|様,yàng|漾,yàng|樣,yàng|怏,yàng|玅,yāo|撽,yāo|幺,yāo|夭,yāo|吆,yāo|妖,yāo|枖,yāo|祅,yāo|訞,yāo|喓,yāo|葽,yāo|楆,yāo|腰,yāo|邀,yāo|宎,yāo|侥,yáo|僥,yáo|蕘,yáo|匋,yáo|恌,yáo|铫,yáo|爻,yáo|尧,yáo|尭,yáo|肴,yáo|垚,yáo|姚,yáo|峣,yáo|轺,yáo|倄,yáo|珧,yáo|窑,yáo|傜,yáo|堯,yáo|揺,yáo|殽,yáo|谣,yáo|軺,yáo|嗂,yáo|媱,yáo|徭,yáo|愮,yáo|搖,yáo|摇,yáo|猺,yáo|遙,yáo|遥,yáo|摿,yáo|暚,yáo|榣,yáo|瑤,yáo|瑶,yáo|飖,yáo|餆,yáo|嶢,yáo|嶤,yáo|徺,yáo|磘,yáo|窯,yáo|餚,yáo|繇,yáo|謠,yáo|謡,yáo|鎐,yáo|鳐,yáo|颻,yáo|蘨,yáo|顤,yáo|鰩,yáo|鷂,yáo|踰,yáo|烑,yáo|窰,yáo|噛,yǎo|仸,yǎo|岆,yǎo|抭,yǎo|杳,yǎo|殀,yǎo|狕,yǎo|苭,yǎo|咬,yǎo|柼,yǎo|窅,yǎo|窈,yǎo|舀,yǎo|偠,yǎo|婹,yǎo|崾,yǎo|溔,yǎo|蓔,yǎo|榚,yǎo|闄,yǎo|騕,yǎo|齩,yǎo|鷕,yǎo|穾,yǎo|鴢,yǎo|烄,yào|药,yào|要,yào|袎,yào|窔,yào|筄,yào|葯,yào|詏,yào|熎,yào|覞,yào|靿,yào|獟,yào|鹞,yào|薬,yào|曜,yào|艞,yào|藥,yào|矅,yào|曣,yào|耀,yào|纅,yào|讑,yào|鑰,yào|怮,yào|箹,yào|钥,yào|籥,yào|亪,ye|椰,yē|暍,yē|噎,yē|潱,yē|蠮,yē|耶,yē|吔,yē|倻,yē|峫,yé|爷,yé|捓,yé|揶,yé|铘,yé|爺,yé|鋣,yé|鎁,yé|擨,yé|蠱,yě|虵,yě|也,yě|冶,yě|埜,yě|野,yě|嘢,yě|漜,yě|壄,yě|瓛,yè|熀,yè|殕,yè|啘,yè|鐷,yè|緤,yè|业,yè|叶,yè|曳,yè|页,yè|邺,yè|夜,yè|亱,yè|枼,yè|洂,yè|頁,yè|捙,yè|晔,yè|枽,yè|烨,yè|偞,yè|掖,yè|液,yè|谒,yè|殗,yè|腋,yè|葉,yè|鄓,yè|墷,yè|楪,yè|業,yè|馌,yè|僷,yè|曄,yè|曅,yè|歋,yè|燁,yè|擖,yè|擛,yè|皣,yè|瞱,yè|靥,yè|嶪,yè|嶫,yè|澲,yè|謁,yè|餣,yè|嚈,yè|擫,yè|曗,yè|瞸,yè|鍱,yè|擪,yè|爗,yè|礏,yè|鎑,yè|饁,yè|鵺,yè|靨,yè|驜,yè|鸈,yè|黦,yè|煠,yè|抴,yè|鄴,yè|膶,yen|岃,yen|袆,yī|褘,yī|一,yī|弌,yī|辷,yī|衤,yī|伊,yī|衣,yī|医,yī|吚,yī|依,yī|祎,yī|咿,yī|洢,yī|猗,yī|畩,yī|郼,yī|铱,yī|壹,yī|揖,yī|欹,yī|蛜,yī|禕,yī|嫛,yī|漪,yī|稦,yī|銥,yī|嬄,yī|噫,yī|夁,yī|瑿,yī|鹥,yī|繄,yī|檹,yī|毉,yī|醫,yī|黟,yī|譩,yī|鷖,yī|黳,yī|悘,yī|壱,yī|耛,yí|拸,yí|訑,yí|釶,yí|鉇,yí|箷,yí|戺,yí|珆,yí|鴺,yí|銕,yí|狏,yí|迱,yí|彵,yí|熈,yí|仪,yí|匜,yí|圯,yí|夷,yí|冝,yí|宐,yí|杝,yí|沂,yí|诒,yí|侇,yí|宜,yí|怡,yí|沶,yí|狋,yí|衪,yí|饴,yí|咦,yí|姨,yí|峓,yí|弬,yí|恞,yí|柂,yí|瓵,yí|荑,yí|贻,yí|迻,yí|宧,yí|巸,yí|扅,yí|桋,yí|眙,yí|胰,yí|袘,yí|痍,yí|移,yí|萓,yí|媐,yí|椬,yí|羠,yí|蛦,yí|詒,yí|貽,yí|遗,yí|暆,yí|椸,yí|誃,yí|跠,yí|頉,yí|颐,yí|飴,yí|疑,yí|儀,yí|熪,yí|遺,yí|嶬,yí|彛,yí|彜,yí|螔,yí|頥,yí|寲,yí|嶷,yí|簃,yí|顊,yí|鮧,yí|彝,yí|彞,yí|謻,yí|鏔,yí|籎,yí|觺,yí|讉,yí|鸃,yí|貤,yí|乁,yí|栘,yí|頤,yí|钀,yǐ|錡,yǐ|裿,yǐ|迤,yǐ|酏,yǐ|乙,yǐ|已,yǐ|以,yǐ|钇,yǐ|佁,yǐ|攺,yǐ|矣,yǐ|苡,yǐ|苢,yǐ|庡,yǐ|舣,yǐ|蚁,yǐ|釔,yǐ|倚,yǐ|扆,yǐ|逘,yǐ|偯,yǐ|崺,yǐ|旑,yǐ|椅,yǐ|鈘,yǐ|鉯,yǐ|鳦,yǐ|旖,yǐ|輢,yǐ|敼,yǐ|螘,yǐ|檥,yǐ|礒,yǐ|艤,yǐ|蟻,yǐ|顗,yǐ|轙,yǐ|齮,yǐ|肊,yǐ|陭,yǐ|嬟,yǐ|醷,yǐ|阤,yǐ|叕,yǐ|锜,yǐ|歖,yǐ|笖,yǐ|昳,yì|睪,yì|欥,yì|輗,yì|掜,yì|儗,yì|謚,yì|紲,yì|絏,yì|辥,yì|义,yì|亿,yì|弋,yì|刈,yì|忆,yì|艺,yì|仡,yì|匇,yì|议,yì|亦,yì|伇,yì|屹,yì|异,yì|忔,yì|芅,yì|伿,yì|佚,yì|劮,yì|呓,yì|坄,yì|役,yì|抑,yì|曵,yì|杙,yì|耴,yì|苅,yì|译,yì|邑,yì|佾,yì|呭,yì|呹,yì|妷,yì|峄,yì|怈,yì|怿,yì|易,yì|枍,yì|泆,yì|炈,yì|绎,yì|诣,yì|驿,yì|俋,yì|奕,yì|帟,yì|帠,yì|弈,yì|枻,yì|浂,yì|玴,yì|疫,yì|羿,yì|衵,yì|轶,yì|唈,yì|垼,yì|悒,yì|挹,yì|栧,yì|栺,yì|欭,yì|浥,yì|浳,yì|益,yì|袣,yì|谊,yì|勚,yì|埸,yì|悥,yì|殹,yì|異,yì|羛,yì|翊,yì|翌,yì|萟,yì|訲,yì|訳,yì|豙,yì|豛,yì|逸,yì|釴,yì|隿,yì|幆,yì|敡,yì|晹,yì|棭,yì|殔,yì|湙,yì|焲,yì|蛡,yì|詍,yì|跇,yì|軼,yì|鈠,yì|骮,yì|亄,yì|意,yì|溢,yì|獈,yì|痬,yì|竩,yì|缢,yì|義,yì|肄,yì|裔,yì|裛,yì|詣,yì|勩,yì|嫕,yì|廙,yì|榏,yì|潩,yì|瘗,yì|膉,yì|蓺,yì|蜴,yì|靾,yì|駅,yì|億,yì|撎,yì|槸,yì|毅,yì|熠,yì|熤,yì|熼,yì|瘞,yì|镒,yì|鹝,yì|鹢,yì|黓,yì|劓,yì|圛,yì|墿,yì|嬑,yì|嶧,yì|憶,yì|懌,yì|曀,yì|殪,yì|澺,yì|燚,yì|瘱,yì|瞖,yì|穓,yì|縊,yì|艗,yì|薏,yì|螠,yì|褹,yì|寱,yì|斁,yì|曎,yì|檍,yì|歝,yì|燡,yì|翳,yì|翼,yì|臆,yì|貖,yì|鮨,yì|癔,yì|藙,yì|藝,yì|贀,yì|鎰,yì|镱,yì|繶,yì|繹,yì|豷,yì|霬,yì|鯣,yì|鶂,yì|鶃,yì|鶍,yì|瀷,yì|蘙,yì|譯,yì|議,yì|醳,yì|饐,yì|囈,yì|鐿,yì|鷁,yì|鷊,yì|襼,yì|驛,yì|鷧,yì|虉,yì|鷾,yì|讛,yì|齸,yì|襗,yì|樴,yì|癦,yì|焬,yì|阣,yì|兿,yì|誼,yì|燱,yì|懿,yì|鮣,yin|乚,yīn|囙,yīn|因,yīn|阥,yīn|阴,yīn|侌,yīn|垔,yīn|姻,yīn|洇,yīn|茵,yīn|荫,yīn|音,yīn|骃,yīn|栶,yīn|殷,yīn|氤,yīn|陰,yīn|凐,yīn|秵,yīn|裀,yīn|铟,yīn|陻,yīn|堙,yīn|婣,yīn|愔,yīn|筃,yīn|絪,yīn|歅,yīn|溵,yīn|禋,yīn|蒑,yīn|蔭,yīn|瘖,yīn|銦,yīn|磤,yīn|緸,yīn|鞇,yīn|諲,yīn|霒,yīn|駰,yīn|噾,yīn|濦,yīn|闉,yīn|霠,yīn|韾,yīn|喑,yīn|玪,yín|伒,yín|乑,yín|吟,yín|犾,yín|苂,yín|斦,yín|泿,yín|圁,yín|峾,yín|烎,yín|狺,yín|珢,yín|粌,yín|荶,yín|訔,yín|唫,yín|婬,yín|寅,yín|崟,yín|崯,yín|淫,yín|訡,yín|银,yín|鈝,yín|滛,yín|碒,yín|鄞,yín|夤,yín|蔩,yín|訚,yín|誾,yín|銀,yín|龈,yín|噖,yín|殥,yín|嚚,yín|檭,yín|蟫,yín|霪,yín|齦,yín|鷣,yín|螾,yín|垠,yín|璌,yín|赺,yǐn|縯,yǐn|尹,yǐn|引,yǐn|吲,yǐn|饮,yǐn|蚓,yǐn|隐,yǐn|淾,yǐn|釿,yǐn|鈏,yǐn|飲,yǐn|隠,yǐn|靷,yǐn|飮,yǐn|朄,yǐn|趛,yǐn|檃,yǐn|瘾,yǐn|隱,yǐn|嶾,yǐn|濥,yǐn|蘟,yǐn|癮,yǐn|讔,yǐn|輑,yǐn|櫽,yǐn|堷,yìn|梀,yìn|隂,yìn|印,yìn|茚,yìn|洕,yìn|胤,yìn|垽,yìn|湚,yìn|猌,yìn|廕,yìn|酳,yìn|慭,yìn|癊,yìn|憖,yìn|憗,yìn|懚,yìn|檼,yìn|韹,yīng|焽,yīng|旲,yīng|应,yīng|応,yīng|英,yīng|偀,yīng|桜,yīng|珱,yīng|莺,yīng|啨,yīng|婴,yīng|媖,yīng|愥,yīng|渶,yīng|朠,yīng|煐,yīng|瑛,yīng|嫈,yīng|碤,yīng|锳,yīng|嘤,yīng|撄,yīng|甇,yīng|緓,yīng|缨,yīng|罂,yīng|蝧,yīng|賏,yīng|樱,yīng|璎,yīng|噟,yīng|罃,yīng|褮,yīng|鴬,yīng|鹦,yīng|嬰,yīng|應,yīng|膺,yīng|韺,yīng|甖,yīng|鹰,yīng|嚶,yīng|孆,yīng|孾,yīng|攖,yīng|瀴,yīng|罌,yīng|蘡,yīng|櫻,yīng|瓔,yīng|礯,yīng|譻,yīng|鶯,yīng|鑍,yīng|纓,yīng|蠳,yīng|鷪,yīng|軈,yīng|鷹,yīng|鸎,yīng|鸚,yīng|謍,yīng|譍,yīng|绬,yīng|鶧,yīng|夃,yíng|俓,yíng|泂,yíng|嵤,yíng|桯,yíng|滎,yíng|鎣,yíng|盁,yíng|迎,yíng|茔,yíng|盈,yíng|荥,yíng|荧,yíng|莹,yíng|萤,yíng|营,yíng|萦,yíng|営,yíng|溁,yíng|溋,yíng|萾,yíng|僌,yíng|塋,yíng|楹,yíng|滢,yíng|蓥,yíng|潆,yíng|熒,yíng|蝇,yíng|瑩,yíng|蝿,yíng|嬴,yíng|營,yíng|縈,yíng|螢,yíng|濙,yíng|濚,yíng|濴,yíng|藀,yíng|覮,yíng|赢,yíng|巆,yíng|攍,yíng|攚,yíng|瀛,yíng|瀠,yíng|蠅,yíng|櫿,yíng|灐,yíng|籝,yíng|灜,yíng|贏,yíng|籯,yíng|耺,yíng|蛍,yíng|瀯,yíng|瀅,yíng|矨,yǐng|郢,yǐng|浧,yǐng|梬,yǐng|颍,yǐng|颕,yǐng|颖,yǐng|摬,yǐng|影,yǐng|潁,yǐng|瘿,yǐng|穎,yǐng|頴,yǐng|巊,yǐng|廮,yǐng|鐛,yǐng|癭,yǐng|鱦,yìng|映,yìng|暎,yìng|硬,yìng|媵,yìng|膡,yìng|鞕,yìng|嚛,yo|哟,yō|唷,yō|喲,yō|拥,yōng|痈,yōng|邕,yōng|庸,yōng|嗈,yōng|鄘,yōng|雍,yōng|墉,yōng|嫞,yōng|慵,yōng|滽,yōng|槦,yōng|牅,yōng|銿,yōng|噰,yōng|壅,yōng|擁,yōng|澭,yōng|郺,yōng|镛,yōng|臃,yōng|癕,yōng|雝,yōng|鏞,yōng|廱,yōng|灉,yōng|饔,yōng|鱅,yōng|鷛,yōng|癰,yōng|鳙,yōng|揘,yóng|喁,yóng|鰫,yóng|嵱,yóng|筩,yǒng|永,yǒng|甬,yǒng|咏,yǒng|怺,yǒng|泳,yǒng|俑,yǒng|勇,yǒng|勈,yǒng|栐,yǒng|埇,yǒng|悀,yǒng|柡,yǒng|涌,yǒng|恿,yǒng|傛,yǒng|惥,yǒng|愑,yǒng|湧,yǒng|硧,yǒng|詠,yǒng|彮,yǒng|愹,yǒng|蛹,yǒng|慂,yǒng|踊,yǒng|禜,yǒng|鲬,yǒng|踴,yǒng|鯒,yǒng|塎,yǒng|佣,yòng|用,yòng|苚,yòng|砽,yòng|醟,yòng|妋,yōu|优,yōu|忧,yōu|攸,yōu|呦,yōu|幽,yōu|悠,yōu|麀,yōu|滺,yōu|憂,yōu|優,yōu|鄾,yōu|嚘,yōu|懮,yōu|瀀,yōu|纋,yōu|耰,yōu|逌,yōu|泈,yōu|櫌,yōu|蓧,yóu|蚘,yóu|揂,yóu|汼,yóu|汓,yóu|蝤,yóu|尣,yóu|冘,yóu|尢,yóu|尤,yóu|由,yóu|沋,yóu|犹,yóu|邮,yóu|怞,yóu|油,yóu|肬,yóu|怣,yóu|斿,yóu|疣,yóu|峳,yóu|浟,yóu|秞,yóu|莜,yóu|莤,yóu|莸,yóu|郵,yóu|铀,yóu|偤,yóu|蚰,yóu|訧,yóu|逰,yóu|游,yóu|猶,yóu|鱿,yóu|楢,yóu|猷,yóu|鈾,yóu|鲉,yóu|輏,yóu|駀,yóu|蕕,yóu|蝣,yóu|魷,yóu|輶,yóu|鮋,yóu|櫾,yóu|邎,yóu|庮,yóu|甴,yóu|遊,yóu|羗,yǒu|脩,yǒu|戭,yǒu|友,yǒu|有,yǒu|丣,yǒu|卣,yǒu|苃,yǒu|酉,yǒu|羑,yǒu|羐,yǒu|莠,yǒu|梄,yǒu|聈,yǒu|脜,yǒu|铕,yǒu|湵,yǒu|蒏,yǒu|蜏,yǒu|銪,yǒu|槱,yǒu|牖,yǒu|牗,yǒu|黝,yǒu|栯,yǒu|禉,yǒu|痏,yòu|褎,yòu|褏,yòu|銹,yòu|柚,yòu|又,yòu|右,yòu|幼,yòu|佑,yòu|侑,yòu|孧,yòu|狖,yòu|糿,yòu|哊,yòu|囿,yòu|姷,yòu|宥,yòu|峟,yòu|牰,yòu|祐,yòu|诱,yòu|迶,yòu|唀,yòu|蚴,yòu|亴,yòu|貁,yòu|釉,yòu|酭,yòu|鼬,yòu|誘,yòu|纡,yū|迂,yū|迃,yū|穻,yū|陓,yū|紆,yū|虶,yū|唹,yū|淤,yū|盓,yū|瘀,yū|箊,yū|亐,yū|丂,yú|桙,yú|婾,yú|媮,yú|悇,yú|汙,yú|汚,yú|鱮,yú|颙,yú|顒,yú|渝,yú|于,yú|邘,yú|伃,yú|余,yú|妤,yú|扵,yú|欤,yú|玗,yú|玙,yú|於,yú|盂,yú|臾,yú|鱼,yú|俞,yú|兪,yú|禺,yú|竽,yú|舁,yú|茰,yú|荢,yú|娛,yú|娯,yú|娱,yú|狳,yú|谀,yú|酑,yú|馀,yú|渔,yú|萸,yú|釪,yú|隃,yú|隅,yú|雩,yú|魚,yú|堣,yú|堬,yú|崳,yú|嵎,yú|嵛,yú|愉,yú|揄,yú|楰,yú|畬,yú|畭,yú|硢,yú|腴,yú|逾,yú|骬,yú|愚,yú|楡,yú|榆,yú|歈,yú|牏,yú|瑜,yú|艅,yú|虞,yú|觎,yú|漁,yú|睮,yú|窬,yú|舆,yú|褕,yú|歶,yú|羭,yú|蕍,yú|蝓,yú|諛,yú|雓,yú|餘,yú|魣,yú|嬩,yú|懙,yú|覦,yú|歟,yú|璵,yú|螸,yú|輿,yú|鍝,yú|礖,yú|謣,yú|髃,yú|鮽,yú|旟,yú|籅,yú|騟,yú|鯲,yú|鰅,yú|鷠,yú|鸆,yú|萮,yú|芌,yú|喩,yú|媀,yú|貗,yú|衧,yú|湡,yú|澞,yú|頨,yǔ|蝺,yǔ|藇,yǔ|予,yǔ|与,yǔ|伛,yǔ|宇,yǔ|屿,yǔ|羽,yǔ|雨,yǔ|俁,yǔ|俣,yǔ|挧,yǔ|禹,yǔ|语,yǔ|圄,yǔ|祤,yǔ|偊,yǔ|匬,yǔ|圉,yǔ|庾,yǔ|敔,yǔ|鄅,yǔ|萭,yǔ|傴,yǔ|寙,yǔ|斞,yǔ|楀,yǔ|瑀,yǔ|瘐,yǔ|與,yǔ|語,yǔ|窳,yǔ|龉,yǔ|噳,yǔ|嶼,yǔ|貐,yǔ|斔,yǔ|麌,yǔ|蘌,yǔ|齬,yǔ|穥,yǔ|峿,yǔ|閼,yù|穀,yù|蟈,yù|僪,yù|鐍,yù|肀,yù|翑,yù|衘,yù|獝,yù|玉,yù|驭,yù|圫,yù|聿,yù|芋,yù|妪,yù|忬,yù|饫,yù|育,yù|郁,yù|彧,yù|昱,yù|狱,yù|秗,yù|俼,yù|峪,yù|浴,yù|砡,yù|钰,yù|预,yù|喐,yù|域,yù|堉,yù|悆,yù|惐,yù|欲,yù|淢,yù|淯,yù|袬,yù|逳,yù|阈,yù|喅,yù|喻,yù|寓,yù|庽,yù|御,yù|棛,yù|棜,yù|棫,yù|焴,yù|琙,yù|矞,yù|裕,yù|遇,yù|飫,yù|馭,yù|鹆,yù|愈,yù|滪,yù|煜,yù|稢,yù|罭,yù|蒮,yù|蓣,yù|誉,yù|鈺,yù|預,yù|嶎,yù|戫,yù|毓,yù|獄,yù|瘉,yù|緎,yù|蜟,yù|蜮,yù|輍,yù|銉,yù|隩,yù|噊,yù|慾,yù|稶,yù|蓹,yù|薁,yù|豫,yù|遹,yù|鋊,yù|鳿,yù|澦,yù|燏,yù|燠,yù|蕷,yù|諭,yù|錥,yù|閾,yù|鴥,yù|鴧,yù|鴪,yù|礇,yù|禦,yù|魊,yù|鹬,yù|癒,yù|礜,yù|篽,yù|繘,yù|鵒,yù|櫲,yù|饇,yù|蘛,yù|譽,yù|轝,yù|鐭,yù|霱,yù|欎,yù|驈,yù|鬻,yù|籞,yù|鱊,yù|鷸,yù|鸒,yù|欝,yù|軉,yù|鬰,yù|鬱,yù|灪,yù|爩,yù|灹,yù|吁,yù|谕,yù|嫗,yù|儥,yù|籲,yù|裷,yuān|嫚,yuān|囦,yuān|鸢,yuān|剈,yuān|冤,yuān|弲,yuān|悁,yuān|眢,yuān|鸳,yuān|寃,yuān|渁,yuān|渆,yuān|渊,yuān|渕,yuān|淵,yuān|葾,yuān|棩,yuān|蒬,yuān|蜎,yuān|鹓,yuān|箢,yuān|鳶,yuān|蜵,yuān|駌,yuān|鋺,yuān|鴛,yuān|嬽,yuān|鵷,yuān|灁,yuān|鼝,yuān|蝝,yuān|鼘,yuān|喛,yuán|楥,yuán|芫,yuán|元,yuán|贠,yuán|邧,yuán|员,yuán|园,yuán|沅,yuán|杬,yuán|垣,yuán|爰,yuán|貟,yuán|原,yuán|員,yuán|圆,yuán|笎,yuán|袁,yuán|厡,yuán|酛,yuán|圎,yuán|援,yuán|湲,yuán|猨,yuán|缘,yuán|鈨,yuán|鼋,yuán|園,yuán|圓,yuán|塬,yuán|媴,yuán|源,yuán|溒,yuán|猿,yuán|獂,yuán|蒝,yuán|榞,yuán|榬,yuán|辕,yuán|緣,yuán|縁,yuán|蝯,yuán|橼,yuán|羱,yuán|薗,yuán|螈,yuán|謜,yuán|轅,yuán|黿,yuán|鎱,yuán|櫞,yuán|邍,yuán|騵,yuán|鶢,yuán|鶰,yuán|厵,yuán|傆,yuán|媛,yuán|褑,yuán|褤,yuán|嫄,yuán|远,yuǎn|盶,yuǎn|遠,yuǎn|逺,yuǎn|肙,yuàn|妴,yuàn|苑,yuàn|怨,yuàn|院,yuàn|垸,yuàn|衏,yuàn|掾,yuàn|瑗,yuàn|禐,yuàn|愿,yuàn|裫,yuàn|噮,yuàn|願,yuàn|哕,yue|噦,yuē|曰,yuē|曱,yuē|约,yuē|約,yuē|矱,yuē|彟,yuē|彠,yuē|矆,yuè|妜,yuè|髺,yuè|哾,yuè|趯,yuè|月,yuè|戉,yuè|汋,yuè|岄,yuè|抈,yuè|礿,yuè|岳,yuè|玥,yuè|恱,yuè|悅,yuè|悦,yuè|蚎,yuè|蚏,yuè|軏,yuè|钺,yuè|阅,yuè|捳,yuè|跀,yuè|跃,yuè|粤,yuè|越,yuè|鈅,yuè|粵,yuè|鉞,yuè|閱,yuè|閲,yuè|嬳,yuè|樾,yuè|篗,yuè|嶽,yuè|籆,yuè|瀹,yuè|蘥,yuè|爚,yuè|禴,yuè|躍,yuè|鸑,yuè|籰,yuè|龥,yuè|鸙,yuè|躒,yuè|刖,yuè|龠,yuè|涒,yūn|轀,yūn|蒀,yūn|煴,yūn|蒕,yūn|熅,yūn|奫,yūn|赟,yūn|頵,yūn|贇,yūn|氲,yūn|氳,yūn|晕,yūn|暈,yūn|云,yún|勻,yún|匀,yún|伝,yún|呍,yún|囩,yún|妘,yún|抣,yún|纭,yún|芸,yún|昀,yún|畇,yún|眃,yún|秐,yún|郧,yún|涢,yún|紜,yún|耘,yún|鄖,yún|雲,yún|愪,yún|溳,yún|筼,yún|蒷,yún|熉,yún|澐,yún|蕓,yún|鋆,yún|橒,yún|篔,yún|縜,yún|繧,yún|荺,yún|沄,yún|允,yǔn|夽,yǔn|狁,yǔn|玧,yǔn|陨,yǔn|殒,yǔn|喗,yǔn|鈗,yǔn|隕,yǔn|殞,yǔn|馻,yǔn|磒,yǔn|霣,yǔn|齫,yǔn|齳,yǔn|抎,yǔn|緷,yùn|孕,yùn|运,yùn|枟,yùn|郓,yùn|恽,yùn|鄆,yùn|酝,yùn|傊,yùn|惲,yùn|愠,yùn|運,yùn|慍,yùn|韫,yùn|韵,yùn|熨,yùn|蕴,yùn|賱,yùn|醖,yùn|醞,yùn|餫,yùn|韗,yùn|韞,yùn|蘊,yùn|韻,yùn|腪,yùn|噈,zā|帀,zā|匝,zā|沞,zā|咂,zā|拶,zā|沯,zā|桚,zā|紮,zā|鉔,zā|臜,zā|臢,zā|砸,zá|韴,zá|雑,zá|襍,zá|雜,zá|雥,zá|囋,zá|杂,zá|咋,zǎ|災,zāi|灾,zāi|甾,zāi|哉,zāi|栽,zāi|烖,zāi|渽,zāi|溨,zāi|睵,zāi|賳,zāi|宰,zǎi|载,zǎi|崽,zǎi|載,zǎi|仔,zǎi|再,zài|在,zài|扗,zài|洅,zài|傤,zài|酨,zài|儎,zài|篸,zān|兂,zān|糌,zān|簪,zān|簮,zān|鐕,zān|撍,zān|咱,zán|偺,zán|喒,zǎn|昝,zǎn|寁,zǎn|儧,zǎn|攒,zǎn|儹,zǎn|趱,zǎn|趲,zǎn|揝,zǎn|穳,zàn|暂,zàn|暫,zàn|賛,zàn|赞,zàn|錾,zàn|鄼,zàn|濽,zàn|蹔,zàn|酂,zàn|瓉,zàn|贊,zàn|鏨,zàn|瓒,zàn|灒,zàn|讃,zàn|瓚,zàn|禶,zàn|襸,zàn|讚,zàn|饡,zàn|酇,zàn|匨,zāng|蔵,zāng|牂,zāng|羘,zāng|赃,zāng|賍,zāng|臧,zāng|賘,zāng|贓,zāng|髒,zāng|贜,zāng|脏,zāng|驵,zǎng|駔,zǎng|奘,zàng|弉,zàng|塟,zàng|葬,zàng|銺,zàng|臓,zàng|臟,zàng|傮,zāo|遭,zāo|糟,zāo|醩,zāo|蹧,zāo|凿,záo|鑿,záo|早,zǎo|枣,zǎo|栆,zǎo|蚤,zǎo|棗,zǎo|璅,zǎo|澡,zǎo|璪,zǎo|薻,zǎo|藻,zǎo|灶,zào|皁,zào|皂,zào|唕,zào|唣,zào|造,zào|梍,zào|慥,zào|煰,zào|艁,zào|噪,zào|簉,zào|燥,zào|竃,zào|譟,zào|趮,zào|竈,zào|躁,zào|啫,zē|伬,zé|则,zé|択,zé|沢,zé|择,zé|泎,zé|泽,zé|责,zé|迮,zé|則,zé|啧,zé|帻,zé|笮,zé|舴,zé|責,zé|溭,zé|嘖,zé|嫧,zé|幘,zé|箦,zé|蔶,zé|樍,zé|歵,zé|諎,zé|赜,zé|擇,zé|皟,zé|瞔,zé|礋,zé|謮,zé|賾,zé|蠌,zé|齚,zé|齰,zé|鸅,zé|讁,zé|葃,zé|澤,zé|仄,zè|夨,zè|庂,zè|汄,zè|昃,zè|昗,zè|捑,zè|崱,zè|贼,zéi|賊,zéi|鲗,zéi|蠈,zéi|鰂,zéi|鱡,zéi|怎,zěn|谮,zèn|囎,zèn|譛,zèn|曽,zēng|増,zēng|鄫,zēng|增,zēng|憎,zēng|缯,zēng|橧,zēng|熷,zēng|璔,zēng|矰,zēng|磳,zēng|罾,zēng|繒,zēng|譄,zēng|鱛,zēng|縡,zēng|鬷,zěng|锃,zèng|鋥,zèng|甑,zèng|赠,zèng|贈,zèng|馇,zha|餷,zha|蹅,zhā|紥,zhā|迊,zhā|抯,zhā|挓,zhā|柤,zhā|哳,zhā|偧,zhā|揸,zhā|渣,zhā|溠,zhā|楂,zhā|劄,zhā|皶,zhā|箚,zhā|樝,zhā|皻,zhā|譇,zhā|齄,zhā|齇,zhā|扎,zhā|摣,zhā|藸,zhā|囃,zhā|喳,zhā|箑,zhá|耫,zhá|札,zhá|轧,zhá|軋,zhá|闸,zhá|蚻,zhá|铡,zhá|牐,zhá|閘,zhá|霅,zhá|鍘,zhá|譗,zhá|挿,zhǎ|揷,zhǎ|厏,zhǎ|苲,zhǎ|砟,zhǎ|鲊,zhǎ|鲝,zhǎ|踷,zhǎ|鮓,zhǎ|鮺,zhǎ|眨,zhǎ|吒,zhà|乍,zhà|诈,zhà|咤,zhà|奓,zhà|炸,zhà|宱,zhà|痄,zhà|蚱,zhà|詐,zhà|搾,zhà|榨,zhà|醡,zhà|拃,zhà|柞,zhà|夈,zhāi|粂,zhāi|捚,zhāi|斋,zhāi|斎,zhāi|榸,zhāi|齋,zhāi|摘,zhāi|檡,zhái|宅,zhái|翟,zhái|窄,zhǎi|鉙,zhǎi|骴,zhài|簀,zhài|债,zhài|砦,zhài|債,zhài|寨,zhài|瘵,zhài|沾,zhān|毡,zhān|旃,zhān|栴,zhān|粘,zhān|蛅,zhān|惉,zhān|詀,zhān|趈,zhān|詹,zhān|閚,zhān|谵,zhān|嶦,zhān|薝,zhān|邅,zhān|霑,zhān|氊,zhān|瞻,zhān|鹯,zhān|旜,zhān|譫,zhān|饘,zhān|鳣,zhān|驙,zhān|魙,zhān|鸇,zhān|覱,zhān|氈,zhān|讝,zhán|斩,zhǎn|飐,zhǎn|展,zhǎn|盏,zhǎn|崭,zhǎn|斬,zhǎn|琖,zhǎn|搌,zhǎn|盞,zhǎn|嶃,zhǎn|嶄,zhǎn|榐,zhǎn|颭,zhǎn|嫸,zhǎn|醆,zhǎn|蹍,zhǎn|欃,zhàn|占,zhàn|佔,zhàn|战,zhàn|栈,zhàn|桟,zhàn|站,zhàn|偡,zhàn|绽,zhàn|菚,zhàn|棧,zhàn|湛,zhàn|戦,zhàn|綻,zhàn|嶘,zhàn|輚,zhàn|戰,zhàn|虥,zhàn|虦,zhàn|轏,zhàn|蘸,zhàn|驏,zhàn|张,zhāng|張,zhāng|章,zhāng|鄣,zhāng|嫜,zhāng|彰,zhāng|慞,zhāng|漳,zhāng|獐,zhāng|粻,zhāng|蔁,zhāng|遧,zhāng|暲,zhāng|樟,zhāng|璋,zhāng|餦,zhāng|蟑,zhāng|鏱,zhāng|騿,zhāng|鱆,zhāng|麞,zhāng|涱,zhāng|傽,zhāng|长,zhǎng|仧,zhǎng|長,zhǎng|镸,zhǎng|仉,zhǎng|涨,zhǎng|掌,zhǎng|漲,zhǎng|幥,zhǎng|礃,zhǎng|鞝,zhǎng|鐣,zhǎng|丈,zhàng|仗,zhàng|扙,zhàng|杖,zhàng|胀,zhàng|账,zhàng|粀,zhàng|帳,zhàng|脹,zhàng|痮,zhàng|障,zhàng|墇,zhàng|嶂,zhàng|幛,zhàng|賬,zhàng|瘬,zhàng|瘴,zhàng|瞕,zhàng|帐,zhàng|鼌,zhāo|鼂,zhāo|謿,zhāo|皽,zhāo|佋,zhāo|钊,zhāo|妱,zhāo|巶,zhāo|招,zhāo|昭,zhāo|炤,zhāo|盄,zhāo|釗,zhāo|鉊,zhāo|駋,zhāo|鍣,zhāo|爫,zhǎo|沼,zhǎo|瑵,zhǎo|爪,zhǎo|找,zhǎo|召,zhào|兆,zhào|诏,zhào|枛,zhào|垗,zhào|狣,zhào|赵,zhào|笊,zhào|肁,zhào|旐,zhào|棹,zhào|罀,zhào|詔,zhào|照,zhào|罩,zhào|肇,zhào|肈,zhào|趙,zhào|曌,zhào|燳,zhào|鮡,zhào|櫂,zhào|瞾,zhào|羄,zhào|啅,zhào|龑,yan|著,zhe|着,zhe|蜇,zhē|嫬,zhē|遮,zhē|嗻,zhē|摂,zhé|歽,zhé|砓,zhé|籷,zhé|虴,zhé|哲,zhé|埑,zhé|粍,zhé|袩,zhé|啠,zhé|悊,zhé|晢,zhé|晣,zhé|辄,zhé|喆,zhé|蛰,zhé|詟,zhé|谪,zhé|摺,zhé|輒,zhé|磔,zhé|輙,zhé|辙,zhé|蟄,zhé|嚞,zhé|謫,zhé|鮿,zhé|轍,zhé|襵,zhé|讋,zhé|厇,zhé|謺,zhé|者,zhě|锗,zhě|赭,zhě|褶,zhě|鍺,zhě|这,zhè|柘,zhè|浙,zhè|這,zhè|淛,zhè|蔗,zhè|樜,zhè|鹧,zhè|蟅,zhè|鷓,zhè|趂,zhēn|贞,zhēn|针,zhēn|侦,zhēn|浈,zhēn|珍,zhēn|珎,zhēn|貞,zhēn|帪,zhēn|栕,zhēn|眞,zhēn|真,zhēn|砧,zhēn|祯,zhēn|針,zhēn|偵,zhēn|敒,zhēn|桭,zhēn|酙,zhēn|寊,zhēn|湞,zhēn|遉,zhēn|搸,zhēn|斟,zhēn|楨,zhēn|獉,zhēn|甄,zhēn|禎,zhēn|蒖,zhēn|蓁,zhēn|鉁,zhēn|靕,zhēn|榛,zhēn|殝,zhēn|瑧,zhēn|禛,zhēn|潧,zhēn|樼,zhēn|澵,zhēn|臻,zhēn|薽,zhēn|錱,zhēn|轃,zhēn|鍖,zhēn|鱵,zhēn|胗,zhēn|侲,zhēn|揕,zhēn|鎭,zhēn|帧,zhēn|幀,zhēn|朾,zhēn|椹,zhēn|桢,zhēn|箴,zhēn|屒,zhén|诊,zhěn|抮,zhěn|枕,zhěn|姫,zhěn|弫,zhěn|昣,zhěn|轸,zhěn|畛,zhěn|疹,zhěn|眕,zhěn|袗,zhěn|聄,zhěn|萙,zhěn|裖,zhěn|覙,zhěn|診,zhěn|軫,zhěn|缜,zhěn|稹,zhěn|駗,zhěn|縝,zhěn|縥,zhěn|辴,zhěn|鬒,zhěn|嫃,zhěn|謓,zhèn|迧,zhèn|圳,zhèn|阵,zhèn|纼,zhèn|挋,zhèn|陣,zhèn|鸩,zhèn|振,zhèn|朕,zhèn|栚,zhèn|紖,zhèn|眹,zhèn|赈,zhèn|塦,zhèn|絼,zhèn|蜄,zhèn|敶,zhèn|誫,zhèn|賑,zhèn|鋴,zhèn|镇,zhèn|鴆,zhèn|鎮,zhèn|震,zhèn|嶒,zhēng|脀,zhēng|凧,zhēng|争,zhēng|佂,zhēng|姃,zhēng|征,zhēng|怔,zhēng|爭,zhēng|峥,zhēng|炡,zhēng|狰,zhēng|烝,zhēng|眐,zhēng|钲,zhēng|埩,zhēng|崝,zhēng|崢,zhēng|猙,zhēng|睁,zhēng|聇,zhēng|铮,zhēng|媜,zhēng|筝,zhēng|徰,zhēng|蒸,zhēng|鉦,zhēng|箏,zhēng|徵,zhēng|踭,zhēng|篜,zhēng|錚,zhēng|鬇,zhēng|癥,zhēng|糽,zhēng|睜,zhēng|氶,zhěng|抍,zhěng|拯,zhěng|塣,zhěng|晸,zhěng|愸,zhěng|撜,zhěng|整,zhěng|憕,zhèng|徎,zhèng|挣,zhèng|掙,zhèng|正,zhèng|证,zhèng|诤,zhèng|郑,zhèng|政,zhèng|症,zhèng|証,zhèng|鄭,zhèng|鴊,zhèng|證,zhèng|諍,zhèng|之,zhī|支,zhī|卮,zhī|汁,zhī|芝,zhī|巵,zhī|枝,zhī|知,zhī|织,zhī|肢,zhī|徔,zhī|栀,zhī|祗,zhī|秓,zhī|秖,zhī|胑,zhī|胝,zhī|衼,zhī|倁,zhī|疷,zhī|祬,zhī|秪,zhī|脂,zhī|隻,zhī|梔,zhī|椥,zhī|搘,zhī|禔,zhī|綕,zhī|榰,zhī|蜘,zhī|馶,zhī|鳷,zhī|謢,zhī|鴲,zhī|織,zhī|蘵,zhī|鼅,zhī|禵,zhī|只,zhī|鉄,zhí|执,zhí|侄,zhí|坧,zhí|直,zhí|姪,zhí|値,zhí|值,zhí|聀,zhí|釞,zhí|埴,zhí|執,zhí|职,zhí|植,zhí|殖,zhí|絷,zhí|跖,zhí|墌,zhí|摭,zhí|馽,zhí|嬂,zhí|慹,zhí|漐,zhí|踯,zhí|膱,zhí|縶,zhí|職,zhí|蟙,zhí|蹠,zhí|軄,zhí|躑,zhí|秇,zhí|埶,zhí|戠,zhí|禃,zhí|茝,zhǐ|絺,zhǐ|觝,zhǐ|徴,zhǐ|止,zhǐ|凪,zhǐ|劧,zhǐ|旨,zhǐ|阯,zhǐ|址,zhǐ|坁,zhǐ|帋,zhǐ|沚,zhǐ|纸,zhǐ|芷,zhǐ|抧,zhǐ|祉,zhǐ|茋,zhǐ|咫,zhǐ|恉,zhǐ|指,zhǐ|枳,zhǐ|洔,zhǐ|砋,zhǐ|轵,zhǐ|淽,zhǐ|疻,zhǐ|紙,zhǐ|訨,zhǐ|趾,zhǐ|軹,zhǐ|黹,zhǐ|酯,zhǐ|藢,zhǐ|襧,zhǐ|汦,zhǐ|胵,zhì|歭,zhì|遟,zhì|迣,zhì|鶨,zhì|亊,zhì|銴,zhì|至,zhì|芖,zhì|志,zhì|忮,zhì|扻,zhì|豸,zhì|厔,zhì|垁,zhì|帙,zhì|帜,zhì|治,zhì|炙,zhì|质,zhì|郅,zhì|俧,zhì|峙,zhì|庢,zhì|庤,zhì|栉,zhì|洷,zhì|祑,zhì|陟,zhì|娡,zhì|徏,zhì|挚,zhì|晊,zhì|桎,zhì|狾,zhì|秩,zhì|致,zhì|袟,zhì|贽,zhì|轾,zhì|徝,zhì|掷,zhì|梽,zhì|猘,zhì|畤,zhì|痔,zhì|秲,zhì|秷,zhì|窒,zhì|紩,zhì|翐,zhì|袠,zhì|觗,zhì|貭,zhì|铚,zhì|鸷,zhì|傂,zhì|崻,zhì|彘,zhì|智,zhì|滞,zhì|痣,zhì|蛭,zhì|骘,zhì|廌,zhì|滍,zhì|稙,zhì|稚,zhì|置,zhì|跱,zhì|輊,zhì|锧,zhì|雉,zhì|槜,zhì|滯,zhì|潌,zhì|瘈,zhì|製,zhì|覟,zhì|誌,zhì|銍,zhì|幟,zhì|憄,zhì|摯,zhì|潪,zhì|熫,zhì|稺,zhì|膣,zhì|觯,zhì|質,zhì|踬,zhì|鋕,zhì|旘,zhì|瀄,zhì|緻,zhì|隲,zhì|鴙,zhì|儨,zhì|劕,zhì|懥,zhì|擲,zhì|櫛,zhì|懫,zhì|贄,zhì|櫍,zhì|瓆,zhì|觶,zhì|騭,zhì|礩,zhì|豑,zhì|騺,zhì|驇,zhì|躓,zhì|鷙,zhì|鑕,zhì|豒,zhì|制,zhì|偫,zhì|筫,zhì|駤,zhì|徸,zhōng|蝩,zhōng|中,zhōng|伀,zhōng|汷,zhōng|刣,zhōng|妐,zhōng|彸,zhōng|忠,zhōng|炂,zhōng|终,zhōng|柊,zhōng|盅,zhōng|衳,zhōng|钟,zhōng|舯,zhōng|衷,zhōng|終,zhōng|鈡,zhōng|幒,zhōng|蔠,zhōng|锺,zhōng|螤,zhōng|鴤,zhōng|螽,zhōng|鍾,zhōng|鼨,zhōng|蹱,zhōng|鐘,zhōng|籦,zhōng|衆,zhōng|迚,zhōng|肿,zhǒng|种,zhǒng|冢,zhǒng|喠,zhǒng|尰,zhǒng|塚,zhǒng|歱,zhǒng|腫,zhǒng|瘇,zhǒng|種,zhǒng|踵,zhǒng|煄,zhǒng|緟,zhòng|仲,zhòng|众,zhòng|妕,zhòng|狆,zhòng|祌,zhòng|茽,zhòng|衶,zhòng|重,zhòng|蚛,zhòng|偅,zhòng|眾,zhòng|堹,zhòng|媑,zhòng|筗,zhòng|諥,zhòng|啁,zhōu|州,zhōu|舟,zhōu|诌,zhōu|侜,zhōu|周,zhōu|洲,zhōu|炿,zhōu|诪,zhōu|珘,zhōu|辀,zhōu|郮,zhōu|婤,zhōu|徟,zhōu|矪,zhōu|週,zhōu|喌,zhōu|粥,zhōu|赒,zhōu|輈,zhōu|銂,zhōu|賙,zhōu|輖,zhōu|霌,zhōu|駲,zhōu|嚋,zhōu|盩,zhōu|謅,zhōu|譸,zhōu|僽,zhōu|諏,zhōu|烐,zhōu|妯,zhóu|轴,zhóu|軸,zhóu|碡,zhóu|肘,zhǒu|帚,zhǒu|菷,zhǒu|晭,zhǒu|睭,zhǒu|箒,zhǒu|鯞,zhǒu|疛,zhǒu|椆,zhòu|詶,zhòu|薵,zhòu|纣,zhòu|伷,zhòu|呪,zhòu|咒,zhòu|宙,zhòu|绉,zhòu|冑,zhòu|咮,zhòu|昼,zhòu|紂,zhòu|胄,zhòu|荮,zhòu|晝,zhòu|皱,zhòu|酎,zhòu|粙,zhòu|葤,zhòu|詋,zhòu|甃,zhòu|皺,zhòu|駎,zhòu|噣,zhòu|縐,zhòu|骤,zhòu|籕,zhòu|籒,zhòu|驟,zhòu|籀,zhòu|蕏,zhū|藷,zhū|朱,zhū|劯,zhū|侏,zhū|诛,zhū|邾,zhū|洙,zhū|茱,zhū|株,zhū|珠,zhū|诸,zhū|猪,zhū|硃,zhū|袾,zhū|铢,zhū|絑,zhū|蛛,zhū|誅,zhū|跦,zhū|槠,zhū|潴,zhū|蝫,zhū|銖,zhū|橥,zhū|諸,zhū|豬,zhū|駯,zhū|鮢,zhū|瀦,zhū|櫧,zhū|櫫,zhū|鼄,zhū|鯺,zhū|蠩,zhū|秼,zhū|騶,zhū|鴸,zhū|薥,zhú|鸀,zhú|朮,zhú|竹,zhú|竺,zhú|炢,zhú|茿,zhú|烛,zhú|逐,zhú|笜,zhú|舳,zhú|瘃,zhú|蓫,zhú|燭,zhú|蠋,zhú|躅,zhú|鱁,zhú|劚,zhú|孎,zhú|灟,zhú|斸,zhú|曯,zhú|欘,zhú|蠾,zhú|钃,zhú|劅,zhú|斀,zhú|爥,zhú|主,zhǔ|宔,zhǔ|拄,zhǔ|砫,zhǔ|罜,zhǔ|渚,zhǔ|煑,zhǔ|煮,zhǔ|詝,zhǔ|嘱,zhǔ|濐,zhǔ|麈,zhǔ|瞩,zhǔ|囑,zhǔ|矚,zhǔ|尌,zhù|伫,zhù|佇,zhù|住,zhù|助,zhù|纻,zhù|苎,zhù|坾,zhù|杼,zhù|苧,zhù|贮,zhù|驻,zhù|壴,zhù|柱,zhù|柷,zhù|殶,zhù|炷,zhù|祝,zhù|疰,zhù|眝,zhù|祩,zhù|竚,zhù|莇,zhù|紵,zhù|紸,zhù|羜,zhù|蛀,zhù|嵀,zhù|筑,zhù|註,zhù|貯,zhù|跓,zhù|軴,zhù|铸,zhù|筯,zhù|鉒,zhù|馵,zhù|墸,zhù|箸,zhù|翥,zhù|樦,zhù|鋳,zhù|駐,zhù|築,zhù|篫,zhù|霔,zhù|麆,zhù|鑄,zhù|櫡,zhù|注,zhù|飳,zhù|抓,zhuā|檛,zhuā|膼,zhuā|髽,zhuā|跩,zhuǎi|睉,zhuài|拽,zhuài|耑,zhuān|专,zhuān|専,zhuān|砖,zhuān|專,zhuān|鄟,zhuān|瑼,zhuān|膞,zhuān|磚,zhuān|諯,zhuān|蟤,zhuān|顓,zhuān|颛,zhuān|转,zhuǎn|転,zhuǎn|竱,zhuǎn|轉,zhuǎn|簨,zhuàn|灷,zhuàn|啭,zhuàn|堟,zhuàn|蒃,zhuàn|瑑,zhuàn|僎,zhuàn|撰,zhuàn|篆,zhuàn|馔,zhuàn|饌,zhuàn|囀,zhuàn|籑,zhuàn|僝,zhuàn|妆,zhuāng|追,zhuī|骓,zhuī|椎,zhuī|锥,zhuī|錐,zhuī|騅,zhuī|鵻,zhuī|沝,zhuǐ|倕,zhuì|埀,zhuì|腏,zhuì|笍,zhuì|娷,zhuì|缀,zhuì|惴,zhuì|甀,zhuì|缒,zhuì|畷,zhuì|膇,zhuì|墜,zhuì|綴,zhuì|赘,zhuì|縋,zhuì|諈,zhuì|醊,zhuì|錣,zhuì|餟,zhuì|礈,zhuì|贅,zhuì|轛,zhuì|鑆,zhuì|坠,zhuì|湻,zhūn|宒,zhūn|迍,zhūn|肫,zhūn|窀,zhūn|谆,zhūn|諄,zhūn|衠,zhūn|訰,zhūn|准,zhǔn|準,zhǔn|綧,zhǔn|稕,zhǔn|凖,zhǔn|鐯,zhuo|拙,zhuō|炪,zhuō|倬,zhuō|捉,zhuō|桌,zhuō|涿,zhuō|棳,zhuō|琸,zhuō|窧,zhuō|槕,zhuō|蠿,zhuō|矠,zhuó|卓,zhuó|圴,zhuó|犳,zhuó|灼,zhuó|妰,zhuó|茁,zhuó|斫,zhuó|浊,zhuó|丵,zhuó|浞,zhuó|诼,zhuó|酌,zhuó|啄,zhuó|娺,zhuó|梲,zhuó|斮,zhuó|晫,zhuó|椓,zhuó|琢,zhuó|斱,zhuó|硺,zhuó|窡,zhuó|罬,zhuó|撯,zhuó|擆,zhuó|斲,zhuó|禚,zhuó|諁,zhuó|諑,zhuó|濁,zhuó|擢,zhuó|斵,zhuó|濯,zhuó|镯,zhuó|鵫,zhuó|灂,zhuó|蠗,zhuó|鐲,zhuó|籗,zhuó|鷟,zhuó|籱,zhuó|烵,zhuó|謶,zhuó|薋,zī|菑,zī|吱,zī|孜,zī|茊,zī|兹,zī|咨,zī|姕,zī|姿,zī|茲,zī|栥,zī|紎,zī|赀,zī|资,zī|崰,zī|淄,zī|秶,zī|缁,zī|谘,zī|赼,zī|嗞,zī|嵫,zī|椔,zī|湽,zī|滋,zī|粢,zī|葘,zī|辎,zī|鄑,zī|孶,zī|禌,zī|觜,zī|貲,zī|資,zī|趑,zī|锱,zī|緇,zī|鈭,zī|镃,zī|龇,zī|輜,zī|鼒,zī|澬,zī|諮,zī|趦,zī|輺,zī|錙,zī|髭,zī|鲻,zī|鍿,zī|頾,zī|頿,zī|鯔,zī|鶅,zī|鰦,zī|齜,zī|訾,zī|訿,zī|芓,zī|孳,zī|鎡,zī|茈,zǐ|泚,zǐ|籽,zǐ|子,zǐ|姉,zǐ|姊,zǐ|杍,zǐ|矷,zǐ|秄,zǐ|呰,zǐ|秭,zǐ|耔,zǐ|虸,zǐ|笫,zǐ|梓,zǐ|釨,zǐ|啙,zǐ|紫,zǐ|滓,zǐ|榟,zǐ|橴,zǐ|自,zì|茡,zì|倳,zì|剚,zì|恣,zì|牸,zì|渍,zì|眥,zì|眦,zì|胔,zì|胾,zì|漬,zì|字,zì|唨,zo|潨,zōng|宗,zōng|倧,zōng|综,zōng|骔,zōng|堫,zōng|嵏,zōng|嵕,zōng|惾,zōng|棕,zōng|猣,zōng|腙,zōng|葼,zōng|朡,zōng|椶,zōng|嵸,zōng|稯,zōng|緃,zōng|熧,zōng|緵,zōng|翪,zōng|蝬,zōng|踨,zōng|踪,zōng|磫,zōng|豵,zōng|蹤,zōng|騌,zōng|鬃,zōng|騣,zōng|鬉,zōng|鯮,zōng|鯼,zōng|鑁,zōng|綜,zōng|潀,zóng|潈,zóng|蓯,zǒng|熜,zǒng|緫,zǒng|总,zǒng|偬,zǒng|捴,zǒng|惣,zǒng|愡,zǒng|揔,zǒng|搃,zǒng|傯,zǒng|蓗,zǒng|摠,zǒng|総,zǒng|燪,zǒng|總,zǒng|鍯,zǒng|鏓,zǒng|縦,zǒng|縂,zǒng|纵,zòng|昮,zòng|疭,zòng|倊,zòng|猔,zòng|碂,zòng|粽,zòng|糉,zòng|瘲,zòng|錝,zòng|縱,zòng|邹,zōu|驺,zōu|诹,zōu|陬,zōu|菆,zōu|棷,zōu|棸,zōu|鄒,zōu|緅,zōu|鄹,zōu|鯫,zōu|黀,zōu|齺,zōu|芻,zōu|鲰,zōu|辶,zǒu|赱,zǒu|走,zǒu|鯐,zǒu|搊,zǒu|奏,zòu|揍,zòu|租,zū|菹,zū|錊,zū|伜,zú|倅,zú|紣,zú|綷,zú|顇,zú|卆,zú|足,zú|卒,zú|哫,zú|崒,zú|崪,zú|族,zú|稡,zú|箤,zú|踤,zú|踿,zú|镞,zú|鏃,zú|诅,zǔ|阻,zǔ|俎,zǔ|爼,zǔ|祖,zǔ|組,zǔ|詛,zǔ|靻,zǔ|鎺,zǔ|组,zǔ|鉆,zuān|劗,zuān|躜,zuān|鑚,zuān|躦,zuān|繤,zuǎn|缵,zuǎn|纂,zuǎn|纉,zuǎn|籫,zuǎn|纘,zuǎn|欑,zuàn|赚,zuàn|賺,zuàn|鑽,zuàn|钻,zuàn|攥,zuàn|厜,zuī|嗺,zuī|樶,zuī|蟕,zuī|纗,zuī|嶉,zuǐ|槯,zuǐ|嶊,zuǐ|噿,zuǐ|濢,zuǐ|璻,zuǐ|嘴,zuǐ|睟,zuì|枠,zuì|栬,zuì|絊,zuì|酔,zuì|晬,zuì|最,zuì|祽,zuì|罪,zuì|辠,zuì|蕞,zuì|醉,zuì|嶵,zuì|檇,zuì|檌,zuì|穝,zuì|墫,zūn|尊,zūn|嶟,zūn|遵,zūn|樽,zūn|繜,zūn|罇,zūn|鶎,zūn|鐏,zūn|鱒,zūn|鷷,zūn|鳟,zūn|僔,zǔn|噂,zǔn|撙,zǔn|譐,zǔn|拵,zùn|捘,zùn|銌,zùn|咗,zuo|昨,zuó|秨,zuó|捽,zuó|椊,zuó|稓,zuó|筰,zuó|鈼,zuó|阝,zuǒ|左,zuǒ|佐,zuǒ|繓,zuǒ|酢,zuò|作,zuò|坐,zuò|阼,zuò|岝,zuò|岞,zuò|怍,zuò|侳,zuò|祚,zuò|胙,zuò|唑,zuò|座,zuò|袏,zuò|做,zuò|葄,zuò|蓙,zuò|飵,zuò|糳,zuò|疮,chuāng|牕,chuāng|噇,chuáng|闖,chuǎng|剏,chuàng|霜,shuāng|欆,shuāng|驦,shuāng|慡,shuǎng|灀,shuàng|窓,chuāng|瘡,chuāng|闯,chuǎng|仺,chuàng|剙,chuàng|雙,shuāng|礵,shuāng|鸘,shuāng|樉,shuǎng|谁,shuí|鹴,shuāng|爽,shuǎng|鏯,shuǎng|孀,shuāng|孇,shuāng|騻,shuāng|焋,zhuàng|幢,zhuàng|撞,zhuàng|隹,zhuī|傱,shuǎn";
				hashtable = new Hashtable();
				foreach (var text2 in text.Split('|'))
				{
					var key = text2.Substring(0, text2.IndexOf(","));
					var value = text2.Substring(1 + text2.IndexOf(","));
					hashtable[key] = value;
				}
			}

			public static string ToPinYin(string hanzi)
			{
				var stringBuilder = new StringBuilder();
				var str = "";
				foreach (var c in hanzi)
				{
					var text = c.ToString();
					if (contain_ch(c.ToString()))
					{
						text = (hashtable[c.ToString()] as string);
					}
					if (contain_en(c.ToString()))
					{
						stringBuilder.Append(text);
					}
					else if (contain_en(str))
					{
						stringBuilder.Append(" " + text + " ");
					}
					else
					{
						stringBuilder.Append(text + " ");
					}
					str = text;
				}
				return stringBuilder.ToString().Replace("  ", " ").Replace("\n ", "\n").Replace(" \n", "\n").Trim();
			}

			private static Hashtable hashtable;
		}

		[Serializable]
		public class TransObj
		{

			// (get) Token: 0x0600014F RID: 335 RVA: 0x00002A1F File Offset: 0x00000C1F
			// (set) Token: 0x06000150 RID: 336 RVA: 0x00002A27 File Offset: 0x00000C27
			public string From
			{
				get
				{
					return from;
				}
				set
				{
					from = value;
				}
			}

			// (get) Token: 0x06000151 RID: 337 RVA: 0x00002A30 File Offset: 0x00000C30
			// (set) Token: 0x06000152 RID: 338 RVA: 0x00002A38 File Offset: 0x00000C38
			public string To
			{
				get
				{
					return to;
				}
				set
				{
					to = value;
				}
			}

			// (get) Token: 0x06000153 RID: 339 RVA: 0x00002A41 File Offset: 0x00000C41
			// (set) Token: 0x06000154 RID: 340 RVA: 0x00002A49 File Offset: 0x00000C49
			public List<TransResult> Data
			{
				get
				{
					return data;
				}
				set
				{
					data = value;
				}
			}

			public List<TransResult> data;

			public string from;

			public string to;
		}

		[Serializable]
		public class TransResult
		{

			// (get) Token: 0x06000156 RID: 342 RVA: 0x00002A52 File Offset: 0x00000C52
			// (set) Token: 0x06000157 RID: 343 RVA: 0x00002A5A File Offset: 0x00000C5A
			public string Src
			{
				get
				{
					return src;
				}
				set
				{
					src = value;
				}
			}

			// (get) Token: 0x06000158 RID: 344 RVA: 0x00002A63 File Offset: 0x00000C63
			// (set) Token: 0x06000159 RID: 345 RVA: 0x00002A6B File Offset: 0x00000C6B
			public string Dst
			{
				get
				{
					return dst;
				}
				set
				{
					dst = value;
				}
			}

			public string dst;

			public string src;
		}

		private class HtmlToText
		{

			static HtmlToText()
			{
				_tags.Add("address", "\n");
				_tags.Add("blockquote", "\n");
				_tags.Add("div", "\n");
				_tags.Add("dl", "\n");
				_tags.Add("fieldset", "\n");
				_tags.Add("form", "\n");
				_tags.Add("h1", "\n");
				_tags.Add("/h1", "\n");
				_tags.Add("h2", "\n");
				_tags.Add("/h2", "\n");
				_tags.Add("h3", "\n");
				_tags.Add("/h3", "\n");
				_tags.Add("h4", "\n");
				_tags.Add("/h4", "\n");
				_tags.Add("h5", "\n");
				_tags.Add("/h5", "\n");
				_tags.Add("h6", "\n");
				_tags.Add("/h6", "\n");
				_tags.Add("p", "\n");
				_tags.Add("/p", "\n");
				_tags.Add("table", "\n");
				_tags.Add("/table", "\n");
				_tags.Add("ul", "\n");
				_tags.Add("/ul", "\n");
				_tags.Add("ol", "\n");
				_tags.Add("/ol", "\n");
				_tags.Add("/li", "\n");
				_tags.Add("br", "\n");
				_tags.Add("/td", "\t");
				_tags.Add("/tr", "\n");
				_tags.Add("/pre", "\n");
				_ignoreTags = new HashSet<string>();
				_ignoreTags.Add("script");
				_ignoreTags.Add("noscript");
				_ignoreTags.Add("style");
				_ignoreTags.Add("object");
			}

			public string Convert(string html)
			{
				_text = new TextBuilder();
				_html = html;
				_pos = 0;
				while (!EndOfText)
				{
					if (Peek() == '<')
					{
						bool flag;
						var text = ParseTag(out flag);
						if (text == "body")
						{
							_text.Clear();
						}
						else if (text == "/body")
						{
							_pos = _html.Length;
						}
						else if (text == "pre")
						{
							_text.Preformatted = true;
							EatWhitespaceToNextLine();
						}
						else if (text == "/pre")
						{
							_text.Preformatted = false;
						}
						string s;
						if (_tags.TryGetValue(text, out s))
						{
							_text.Write(s);
						}
						if (_ignoreTags.Contains(text))
						{
							EatInnerContent(text);
						}
					}
					else if (char.IsWhiteSpace(Peek()))
					{
						_text.Write(_text.Preformatted ? Peek() : ' ');
						MoveAhead();
					}
					else
					{
						_text.Write(Peek());
						MoveAhead();
					}
				}
				return HttpUtility.HtmlDecode(_text.ToString());
			}

			protected string ParseTag(out bool selfClosing)
			{
				var result = string.Empty;
				selfClosing = false;
				if (Peek() == '<')
				{
					MoveAhead();
					EatWhitespace();
					var pos = _pos;
					if (Peek() == '/')
					{
						MoveAhead();
					}
					while (!EndOfText && !char.IsWhiteSpace(Peek()) && Peek() != '/' && Peek() != '>')
					{
						MoveAhead();
					}
					result = _html.Substring(pos, _pos - pos).ToLower();
					while (!EndOfText && Peek() != '>')
					{
						if (Peek() == '"' || Peek() == '\'')
						{
							EatQuotedValue();
						}
						else
						{
							if (Peek() == '/')
							{
								selfClosing = true;
							}
							MoveAhead();
						}
					}
					MoveAhead();
				}
				return result;
			}

			protected void EatInnerContent(string tag)
			{
				var b = "/" + tag;
				while (!EndOfText)
				{
					if (Peek() == '<')
					{
						bool flag;
						if (ParseTag(out flag) == b)
						{
							return;
						}
						if (!flag && !tag.StartsWith("/"))
						{
							EatInnerContent(tag);
						}
					}
					else
					{
						MoveAhead();
					}
				}
			}

			// (get) Token: 0x0600015F RID: 351 RVA: 0x00002A74 File Offset: 0x00000C74
			protected bool EndOfText
			{
				get
				{
					return _pos >= _html.Length;
				}
			}

			protected char Peek()
			{
				if (_pos >= _html.Length)
				{
					return '\0';
				}
				return _html[_pos];
			}

			protected void MoveAhead()
			{
				_pos = Math.Min(_pos + 1, _html.Length);
			}

			protected void EatWhitespace()
			{
				while (char.IsWhiteSpace(Peek()))
				{
					MoveAhead();
				}
			}

			protected void EatWhitespaceToNextLine()
			{
				while (char.IsWhiteSpace(Peek()))
				{
					var num = (int)Peek();
					MoveAhead();
					if (num == 10)
					{
						break;
					}
				}
			}

			protected void EatQuotedValue()
			{
				var c = Peek();
				if (c == '"' || c == '\'')
				{
					MoveAhead();
					var pos = _pos;
					_pos = _html.IndexOfAny(new[]
					{
						c,
						'\r',
						'\n'
					}, _pos);
					if (_pos < 0)
					{
						_pos = _html.Length;
						return;
					}
					MoveAhead();
				}
			}

			protected static Dictionary<string, string> _tags = new Dictionary<string, string>();

			protected static HashSet<string> _ignoreTags;

			protected TextBuilder _text;

			protected string _html;

			protected int _pos;

			protected class TextBuilder
			{

				public TextBuilder()
				{
					_text = new StringBuilder();
					_currLine = new StringBuilder();
					_emptyLines = 0;
					_preformatted = false;
				}

				// (get) Token: 0x06000167 RID: 359 RVA: 0x00002B38 File Offset: 0x00000D38
				// (set) Token: 0x06000168 RID: 360 RVA: 0x00002B40 File Offset: 0x00000D40
				public bool Preformatted
				{
					get
					{
						return _preformatted;
					}
					set
					{
						if (value)
						{
							if (_currLine.Length > 0)
							{
								FlushCurrLine();
							}
							_emptyLines = 0;
						}
						_preformatted = value;
					}
				}

				public void Clear()
				{
					_text.Length = 0;
					_currLine.Length = 0;
					_emptyLines = 0;
				}

				public void Write(string s)
				{
					foreach (var c in s)
					{
						Write(c);
					}
				}

				public void Write(char c)
				{
					if (_preformatted)
					{
						_text.Append(c);
						return;
					}
					if (c != '\r')
					{
						if (c == '\n')
						{
							FlushCurrLine();
							return;
						}
						if (char.IsWhiteSpace(c))
						{
							var length = _currLine.Length;
							if (length == 0 || !char.IsWhiteSpace(_currLine[length - 1]))
							{
								_currLine.Append(' ');
								return;
							}
						}
						else
						{
							_currLine.Append(c);
						}
					}
				}

				protected void FlushCurrLine()
				{
					var text = _currLine.ToString().Trim();
					if (text.Replace("\u00a0", string.Empty).Length == 0)
					{
						_emptyLines++;
						if (_emptyLines < 2 && _text.Length > 0)
						{
							_text.AppendLine(text);
						}
					}
					else
					{
						_emptyLines = 0;
						_text.AppendLine(text);
					}
					_currLine.Length = 0;
				}

				public override string ToString()
				{
					if (_currLine.Length > 0)
					{
						FlushCurrLine();
					}
					return _text.ToString();
				}

				private StringBuilder _text;

				private StringBuilder _currLine;

				private int _emptyLines;

				private bool _preformatted;
			}
		}
	}
}
