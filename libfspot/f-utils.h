/* -*- Mode: C; indent-tabs-mode: t; c-basic-offset: 8; tab-width: 8 -*- */
/* f-utils.h
 *
 * Copyright (C) 2003  Ettore Perazzoli
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 *
 * Author: Ettore Perazzoli <ettore@ximian.com>
 */

#ifndef F_UTILS_H
#define F_UTILS_H

#include <gdk-pixbuf/gdk-pixbuf.h>
#include <math.h>


#define F_MAKE_TYPE(name, underscore_name)							\
	GType											\
	underscore_name##_get_type (void)							\
	{											\
		static GType type = 0;								\
												\
		if (type == 0) {								\
			static const GTypeInfo info = {						\
				sizeof (name##Class),						\
				(GBaseInitFunc) NULL,						\
				(GBaseFinalizeFunc) NULL,					\
				(GClassInitFunc) class_init,					\
				NULL,           /* class_finalize */				\
				NULL,           /* class_data */				\
				sizeof (name),							\
				0,               /* n_preallocs */				\
				(GInstanceInitFunc) init,					\
			};									\
												\
			type = g_type_register_static (PARENT_TYPE, #name, &info, 0);		\
		}										\
												\
		return type;									\
	}
	
#define F_MAKE_TYPE_WITH_ERROR(name, underscore_name)						\
	F_MAKE_TYPE (name, underscore_name)							\
	GQuark											\
	underscore_name##_error_quark (void)							\
	{											\
		static GQuark q = 0;								\
												\
		if (q == 0)									\
			q = g_quark_from_static_string (#underscore_name "_error_quark");	\
												\
		return q;									\
	}


/* Refcounting macros.  Note that some of these evaluates OBJ multiple times,
   which is not ideal...  So you want to pass only variables (not functions) to
   them.  */

#define F_UNREF(obj)				\
	G_STMT_START {				\
		if ((obj) != NULL) {		\
			g_object_unref (obj);	\
			(obj) = NULL;		\
		} 				\
	} G_STMT_END

#define F_REF(obj)					\
	((obj) != NULL ? (g_object_ref (obj), (obj))	\
		       : NULL)

#define F_ASSIGN(dest, src)			\
	G_STMT_START {				\
		F_UNREF(dest);			\
		(dest) = F_REF(src);		\
	} G_STMT_END

#define F_WEAK_NULL(dest)									\
	G_STMT_START {										\
		if ((dest) != NULL) {								\
			g_object_remove_weak_pointer (G_OBJECT (dest), (void **) & (dest));	\
			(dest) = NULL;								\
		}										\
	} G_STMT_END

#define F_WEAK_ASSIGN(dest, src)								\
	G_STMT_START {										\
		F_WEAK_NULL(dest);								\
		if ((src) != NULL) {								\
			(dest) = (src);								\
			g_object_add_weak_pointer (G_OBJECT (dest), (void **) & (dest));	\
		}										\
	} G_STMT_END

#define F_BOOLEAN_MEMBER(name)				\
	unsigned int name : 1

#define F_LIST_FOREACH(list, iterator) 		\
	for ((iterator) = (list); (iterator) != NULL; (iterator) = (iterator)->next)

#define F_DOUBLE_EQUAL(a, b)			\
	(fabs (a - b) < 1e-6)


/* Build a relative path from START_PATH to DESTINATION_PATH.  */
char *f_build_relative_path (const char *start_path,
			     const char *destination_path);

#endif /* F_UTILS_H */
