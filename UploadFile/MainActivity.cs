using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using XFileUpload;
using System.Threading;
using System.Linq;

namespace UploadFile
{
    [Activity(Label = "UploadFile", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private ProgressBar pb;
        private ByteSocket bs;
        string uploadpath = "http://172.16.101.44/Upload.ashx";
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            Button button = FindViewById<Button>(Resource.Id.MyButton);
            pb = FindViewById<ProgressBar>(Resource.Id.progressBar1);

            FindViewById<Button>(Resource.Id.btnStop).Click += (e, s) =>
            {
                if(bs != null)
                {
                    bs.IsStop = true;
                }
            };

            button.Click += delegate { ShowPictures(); };
        }

        protected void ShowPictures()
        {
            Intent intent = new Intent(Intent.ActionGetContent);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("image/*");
            StartActivityForResult(intent, 1);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok)
            {
                if (data != null && data.Data != null)
                {
                    var cursor = ContentResolver.Query(data.Data, new string[] { "_data" }, null, null, null);
                    if (cursor != null && cursor.MoveToFirst())
                    {
                        String path = cursor.GetString(cursor.GetColumnIndex("_data"));
                        if (File.Exists(path))
                        {
                            UploadFile(path);
                        }
                    }
                }
            }
        }

        protected void UploadFile(string path)
        {
            bs = new ByteSocket(uploadpath, path, "test", "test.jpg", "image/jpeg");
            bs.OnException += bs_OnException;
            bs.OnFinished += bs_OnFinished;
            bs.OnSend += bs_OnSend;
            bs.Upload();
        }

        protected void bs_OnSend(int arg1, int arg2)
        {
            RunOnUiThread(() =>
            {
                if (pb.Max != arg1)
                {
                    pb.Max = arg1;
                    pb.Progress = 0;
                }
                pb.Progress += arg2;
            });
        }

        protected void bs_OnFinished(string id)
        {
            
            RunOnUiThread(() =>
            {
                Toast.MakeText(this, "完成", ToastLength.Short).Show();
            });
        }

        protected void bs_OnException(Exception obj)
        {
            RunOnUiThread(() =>
            {
                var t = obj;
                Toast.MakeText(this, "出错", ToastLength.Short).Show();
            });
        }
    }
}
