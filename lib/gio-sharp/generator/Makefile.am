include $(top_srcdir)/Makefile.include

CLEANFILES =				\
	gapi_codegen.exe		\
	gapi_codegen.exe.mdb

sources = 				\
	AliasGen.cs			\
	BoxedGen.cs			\
	ByRefGen.cs			\
	CallbackGen.cs			\
	ChildProperty.cs		\
	ClassBase.cs			\
	ClassGen.cs			\
	CodeGenerator.cs		\
	ConstFilenameGen.cs		\
	ConstStringGen.cs		\
	Ctor.cs				\
	EnumGen.cs			\
	FieldBase.cs			\
	GenBase.cs			\
	GenerationInfo.cs		\
	HandleBase.cs			\
	IAccessor.cs			\
	IGeneratable.cs			\
	IManualMarshaler.cs		\
	InterfaceGen.cs			\
	LPGen.cs			\
	LPUGen.cs			\
	ManagedCallString.cs		\
	ManualGen.cs			\
	MarshalGen.cs			\
	MethodBase.cs			\
	MethodBody.cs			\
	Method.cs			\
	ObjectField.cs			\
	ObjectBase.cs			\
	ObjectGen.cs			\
	OpaqueGen.cs			\
	Parameters.cs			\
	Parser.cs			\
	Property.cs			\
	PropertyBase.cs			\
	ReturnValue.cs			\
	Signal.cs			\
	Signature.cs			\
	SimpleBase.cs			\
	SimpleGen.cs			\
	Statistics.cs			\
	StructBase.cs			\
	StructField.cs			\
	StructGen.cs			\
	SymbolTable.cs			\
	VirtualMethod.cs		\
	VMSignature.cs

build_sources = $(addprefix $(srcdir)/, $(sources))
dist_sources = $(sources)

EXTRA_DIST =					\
	$(dist_sources)

all: gapi_codegen.exe

gapi_codegen.exe: $(build_sources)
	@echo -e "\n*** Building the generator"
	$(CSC) /out:gapi_codegen.exe $(build_sources)

