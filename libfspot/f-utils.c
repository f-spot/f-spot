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

#include <config.h>

#include "f-utils.h"

#include <glib/gmacros.h>


char *
f_build_relative_path (const char *start_path,
		       const char *destination_path)
{
	const char *sp, *dp;
	GString *relative_path;
	gboolean need_separator;
	char *retval;

	g_return_val_if_fail (g_path_is_absolute (start_path), NULL);
	g_return_val_if_fail (g_path_is_absolute (destination_path), NULL);

	sp = start_path;
	dp = destination_path;

	/* Look for a common subpath at the beginning of the paths.  */

	while (*sp == *dp && *sp != 0) {
		sp ++;
		dp ++;
	}

	if (*sp == 0 && *dp == 0)
		return g_strdup ("");

	/* Roll back to the path element.  This is guarranteed to not run past
	   the beginning of the string because of the g_path_is_absolute()
	   checks above.  */

	while (*sp != G_DIR_SEPARATOR && *sp != 0)
		sp --;
	while (*dp != G_DIR_SEPARATOR && *dp != 0)
		dp --;

	g_assert (*dp == G_DIR_SEPARATOR || *dp == 0);
	g_assert (*sp == G_DIR_SEPARATOR || *sp == 0);

	/* Start constructing the string by adding one ".." for each path
	   component in the source path that is not in the destination
	   path.  */

	relative_path = g_string_new ("");

	need_separator = FALSE;
	while (*sp != 0) {
		sp ++;

		if (*sp == G_DIR_SEPARATOR || *sp == 0) {
			while (*sp == G_DIR_SEPARATOR)
				sp ++;

			if (need_separator)
				g_string_append (relative_path, G_DIR_SEPARATOR_S);
				
			g_string_append (relative_path, "..");
			need_separator = TRUE;
		}
	}

	/* Add the rest of the path elements, if any.  */

	if (*dp != 0) {
  		/*  (Notice that DP is guaranteed to point to either a null character
		    or to a separator so we don't need to add it ourselves.)  */
  		if (need_separator)
			g_string_append (relative_path, dp);
		else
			g_string_append (relative_path, dp + 1);
	}

	retval = relative_path->str;
	g_string_free (relative_path, FALSE);

	return retval;
}
