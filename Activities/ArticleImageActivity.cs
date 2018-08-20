using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Graphics;
using Android.Support.V4.Content;
using Android.Views;

namespace VorratsUebersicht
{
    [Activity(Label = "ArticleImageActivity")]
    public class ArticleImageActivity : Activity
    {
        int articleId;
        ImageView imageView;

        public static readonly int PickImageId = 1000;
        public static readonly int TakePhotoId = 1001;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ArticleImage);

            // ActionBar Hintergrund Farbe setzen
            var backgroundPaint = ContextCompat.GetDrawable(this, Resource.Color.Application_ActionBar_Background);
            backgroundPaint.SetBounds(0, 0, 10, 10);
            ActionBar.SetBackgroundDrawable(backgroundPaint);
            ActionBar.SetDisplayHomeAsUpEnabled(true);

            this.articleId = Intent.GetIntExtra    ("ArticleId", 0);
            bool large     = Intent.GetBooleanExtra("Large", true);
            string text    = Database.GetArticleName(this.articleId);

            this.imageView  = FindViewById<ImageView> (Resource.Id.ArticleImage_Image);

            ActionBar.Title = text;
            ActionBar.SetHomeButtonEnabled(true);
            ActionBar.SetIcon(Resource.Drawable.ic_photo_camera_white_24dp);

            this.ShowPictureFromDatabase(large);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    this.OnBackPressed();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ShowPictureFromDatabase(bool showLarge)
        {
            if (this.articleId == 0)
            {
                this.imageView.SetImageResource(Resource.Drawable.ic_photo_camera_white_24dp);
                return;
            }

            Article article = Database.GetArticleImage(this.articleId, showLarge);
            if (article == null)
                return;

            if (article.Image != null)
            {
                string message = string.Empty;
                try
                {
                    Bitmap image= BitmapFactory.DecodeByteArray (article.Image, 0, article.Image.Length);

                    // Bild um 90 Grad drehen
                    //Matrix mat = new Matrix();
                    //mat.PostRotate(90);
                    //Bitmap bMapRotate = Bitmap.CreateBitmap(image, 0, 0, image.Width, image.Height, mat, true);
                    //this.imageView.SetImageBitmap(bMapRotate);

                    this.imageView.SetImageBitmap(image);

                    message = string.Format("Bild: {0:n0} X {1:n0} ({2:n0})", image.Height, image.Width, Tools.ToFuzzyByteString(image.ByteCount));
                    image = null;
                }
                catch (Exception e)
                {
                    message = e.Message;
                    this.imageView.SetImageResource(Resource.Drawable.baseline_error_outline_black_24);
                }               

                FindViewById<TextView> (Resource.Id.ArticleImage_Info).Text = message;
                article = null;
            }
            else
                this.imageView.SetImageResource(Resource.Drawable.ic_photo_camera_black_24dp);
        }
    }
}