/*
 * context.c
 *
 * Author(s):
 *	Stephane Delcroix <stephan @delcroix.org>
 *
 * Copyright (c) 2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */

#include <stdlib.h>
#include <gphoto2-context.h>
#include <stdarg.h>

struct _GPGlueContext;
typedef struct _GPGlueContext GPGlueContext;

typedef unsigned int (*GPGlueContextProgressStartFunc) (GPGlueContext *gluecontext, float target, const char *message, void *data);
typedef void (*GPGlueContextProgressUpdateFunc) (GPGlueContext *gluecontext, unsigned int id, float current, void *data);
typedef void (*GPGlueContextProgressStopFunc) (GPGlueContext *gluecontext, unsigned int id, void *data);

typedef void (*GPGlueContextErrorFunc) (GPGlueContext *gluecontext, const char *message, void *data);
typedef void (*GPGlueContextStatusFunc) (GPGlueContext *gluecontext, const char *message, void *data);
typedef void (*GPGlueContextMessageFunc) (GPGlueContext *gluecontext, const char *message, void *data);

struct _GPGlueContext {
	GPContext *context;
	GPGlueContextProgressStartFunc start_func;
	GPGlueContextProgressUpdateFunc update_func;
	GPGlueContextProgressStopFunc stop_func;
	void *progress_data;
	GPGlueContextErrorFunc error_func;
	void *error_data;
	GPGlueContextStatusFunc status_func;
	void *status_data;
	GPGlueContextMessageFunc message_func;
	void *message_data;
};

GPGlueContext *
gp_glue_context_new (GPContext *context)
{
	GPGlueContext *gluecontext = malloc (sizeof (GPGlueContext));
	gluecontext->context = context;
	return gluecontext;
}

void
gp_glue_context_free (GPGlueContext *gluecontext)
{
	free (gluecontext);
}

unsigned int
gp_glue_start_func_wrapper (GPContext *context, float target, const char *format, va_list args, void *data)
{
	GPGlueContext *gluecontext = (GPGlueContext *)data;
	unsigned int ret;
	char buf[256];
	vsnprintf (buf, 256, format, args);
	ret = gluecontext->start_func (gluecontext, target, buf, gluecontext->progress_data);

	return ret;
}

void
gp_glue_update_func_wrapper (GPContext *context, unsigned int id, float current, void *data)
{
	GPGlueContext *gluecontext = (GPGlueContext *)data;
	gluecontext->update_func (gluecontext, id, current, gluecontext->progress_data);
}

void
gp_glue_stop_func_wrapper (GPContext *context, unsigned int id, void *data)
{
	GPGlueContext *gluecontext = (GPGlueContext *)data;
	gluecontext->stop_func (gluecontext, id, gluecontext->progress_data);
}

void
gp_glue_context_set_progress_funcs (GPGlueContext *gluecontext, GPGlueContextProgressStartFunc start_func, GPGlueContextProgressUpdateFunc update_func, GPGlueContextProgressStopFunc stop_func, void *data)
{
	gluecontext->start_func = start_func;
	gluecontext->update_func = update_func;
	gluecontext->stop_func = stop_func;
	gluecontext->progress_data = data;

	gp_context_set_progress_funcs (gluecontext->context,
				       gp_glue_start_func_wrapper,
				       gp_glue_update_func_wrapper,
				       gp_glue_stop_func_wrapper,
				       gluecontext);
}

void
gp_glue_error_func_wrapper (GPContext *context, const char *format, va_list args, void *data)
{
	GPGlueContext *gluecontext = (GPGlueContext *)data;
	char buf[256];
	vsnprintf (buf, 256, format, args);
	gluecontext->error_func (gluecontext, buf, gluecontext->error_data);
	
}

void
gp_glue_context_set_error_func (GPGlueContext *gluecontext, GPGlueContextErrorFunc error_func, void *data)
{
	gluecontext->error_func = error_func;
	gluecontext->error_data = data;

	gp_context_set_error_func (gluecontext->context, gp_glue_error_func_wrapper, gluecontext);
}

void
gp_glue_status_func_wrapper (GPContext *context, const char *format, va_list args, void *data)
{
	GPGlueContext *gluecontext = (GPGlueContext *)data;
	char buf[256];
	vsnprintf (buf, 256, format, args);
	gluecontext->status_func (gluecontext, buf, gluecontext->status_data);
	
}

void
gp_glue_context_set_status_func (GPGlueContext *gluecontext, GPGlueContextStatusFunc status_func, void *data)
{
	gluecontext->status_func = status_func;
	gluecontext->status_data = data;

	gp_context_set_status_func (gluecontext->context, gp_glue_status_func_wrapper, gluecontext);
}

void
gp_glue_message_func_wrapper (GPContext *context, const char *format, va_list args, void *data)
{
	GPGlueContext *gluecontext = (GPGlueContext *)data;
	char buf[256];
	vsnprintf (buf, 256, format, args);
	gluecontext->message_func (gluecontext, buf, gluecontext->message_data);
	
}

void
gp_glue_context_set_message_func (GPGlueContext *gluecontext, GPGlueContextMessageFunc message_func, void *data)
{
	gluecontext->message_func = message_func;
	gluecontext->message_data = data;

	gp_context_set_message_func (gluecontext->context, gp_glue_message_func_wrapper, gluecontext);
}
