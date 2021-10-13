// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FSpot.Imaging;
using FSpot.Thumbnail;

namespace FSpot.Services
{
	public class PhotoService
	{
		readonly IImageFileFactory imageFileFactory;
		readonly IThumbnailService thumbnailService;

		public PhotoService (/*Models.Photo photo, */IImageFileFactory imageFactory, IThumbnailService thumbnailService)
		{
			this.imageFileFactory = imageFactory;
			this.thumbnailService = thumbnailService;

			//Photo = photo;

			//time = DateTimeUtil.ToDateTime (unix_time);
			//Tags = new List<Tag> ();

			//description = string.Empty;
			//rating = 0;
		}


	}
}
