/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using MagnifierApp.Models;
using MagnifierApp.Resources;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MagnifierApp.Pages
{
    public class InfoPageViewModel
    {
        public class Info
        {
            public string Title { get; set; }
            public string Value { get; set; }
        }

        public ObservableCollection<Info> Infos { get; private set; }

        public InfoPageViewModel()
        {
            Infos = new ObservableCollection<Info>();

            PopulateInfos();
        }

        private void PopulateInfos()
        {
            if (PhotoModel.Singleton.Image != null)
            {
                PhotoModel.Singleton.Image.Position = 0;

                using (var stream = new MemoryStream())
                {
                    PhotoModel.Singleton.Image.CopyTo(stream);

                    stream.Position = 0;

                    try
                    {
                        TryReadExifInfo(stream);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void TryReadExifInfo(Stream stream)
        {
            using (var reader = new ExifLib.ExifReader(stream))
            {
                PrintSupportedExifInfo(reader);

                // Aperture

                double aperture;

                if (reader.GetTagValue(ExifLib.ExifTags.FNumber, out aperture))
                {
                    Infos.Add(new Info()
                        {
                            Title = AppResources.InfoPage_ApertureTitleTextBlock_Text,
                            Value = "F" + Math.Round(aperture, 1).ToString().Replace(',', '.')
                        });
                }

                // Exposure time

                double exposureTime;

                if (reader.GetTagValue(ExifLib.ExifTags.ExposureTime, out exposureTime))
                {
                    Infos.Add(new Info()
                        {
                            Title = AppResources.InfoPage_ExposureTimeTitleTextBlock_Text,
                            Value = String.Format(AppResources.InfoPage_ExposureTimeValueFormatTextBlock_Text, Math.Round(exposureTime, 2).ToString())
                        });
                }

                // ISO

                UInt16 iso;

                if (reader.GetTagValue(ExifLib.ExifTags.ISOSpeedRatings, out iso) && iso > 0)
                {
                    Infos.Add(new Info()
                        {
                            Title = AppResources.InfoPage_IsoTitleTextBlock_Text,
                            Value = iso.ToString()
                        });
                }

                // Flash (simplified)

                UInt16 flash;

                if (reader.GetTagValue(ExifLib.ExifTags.Flash, out flash))
                {
                    var bytes = BitConverter.GetBytes(flash);

                    Infos.Add(new Info()
                        {
                            Title = AppResources.InfoPage_FlashTitleTextBlock_Text,
                            Value = (bytes[0] & 0x01) != 0x00 ?
                                AppResources.InfoPage_FlashValueFiredTextBlock_Text :
                                AppResources.InfoPage_FlashValueNotFiredTextBlock_Text
                        });
                }

                // Date and time

                DateTime dateTime;

                if (reader.GetTagValue(ExifLib.ExifTags.DateTimeOriginal, out dateTime))
                {
                    Infos.Add(new Info()
                        {
                            Title = AppResources.InfoPage_DateTimeTitleTextBlock_Text,
                            Value = dateTime.ToLocalTime().ToString()
                        });
                }

                // Dimensions

                UInt32 x;
                UInt32 y;

                if (reader.GetTagValue(ExifLib.ExifTags.PixelXDimension, out x) && x > 0 &&
                    reader.GetTagValue(ExifLib.ExifTags.PixelYDimension, out y) && y > 0)
                {
                    Infos.Add(new Info()
                        {
                            Title = AppResources.InfoPage_DimensionsTitleTextBlock_Text,
                            Value = x + " x " + y
                        });
                }

                // Make

                string make;

                if (reader.GetTagValue(ExifLib.ExifTags.Make, out make) && make.Length > 0)
                {
                    Infos.Add(new Info()
                        {
                            Title = AppResources.InfoPage_MakeTitleTextBlock_Text,
                            Value = make
                        });
                }

                // Model

                string model;

                if (reader.GetTagValue(ExifLib.ExifTags.Model, out model) && model.Length > 0)
                {
                    Infos.Add(new Info()
                        { 
                            Title = AppResources.InfoPage_ModelTitleTextBlock_Text,
                            Value = model
                        });
                }

                // Software

                string software;

                if (reader.GetTagValue(ExifLib.ExifTags.Software, out software) && software.Length > 0)
                {
                    Infos.Add(new Info()
                        {
                            Title = AppResources.InfoPage_SoftwareTitleTextBlock_Text,
                            Value = software
                        });
                }
            }
        }

        private static void PrintSupportedExifInfo(ExifLib.ExifReader reader)
        {
            foreach (ExifLib.ExifTags t in GetEnumValues<ExifLib.ExifTags>())
            {
                object value;

                try
                {
                    if (reader.GetTagValue(t, out value))
                    {
                        System.Diagnostics.Debug.WriteLine(t.ToString() + " = " + value.ToString() + " (" + value.GetType().ToString() + ")");
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static T[] GetEnumValues<T>()
        {
            var type = typeof(T);

            if (!type.IsEnum)
                throw new ArgumentException("Type '" + type.Name + "' is not an enum");

            return (
              from field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
              where field.IsLiteral
              select (T)field.GetValue(null)
            ).ToArray();
        }
    }
}
