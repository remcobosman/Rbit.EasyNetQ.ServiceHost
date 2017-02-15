USE [BRUTST_RabbitManagement]
GO
/****** Object:  Table [dbo].[AuditLog]    Script Date: 4/4/2016 9:10:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AuditLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CorrelationId] [uniqueidentifier] NOT NULL,
	[ProducerName] [nvarchar](255) NOT NULL,
	[Direction] [nvarchar](10) NOT NULL,
	[Properties] [nvarchar](max) NOT NULL,
	[Payload] [nvarchar](max) NOT NULL,
	[DateTime] [datetime] NOT NULL,
	[CreatedOn] [datetime] NOT NULL CONSTRAINT [DF_Audit_CreatedOn]  DEFAULT (getdate()),
	[Type] [nvarchar](max) NOT NULL,
	[TypeAssembly] [nvarchar](max) NOT NULL,
	[MessageId] [uniqueidentifier] NULL,
	[RunId] [uniqueidentifier] NULL,
	[HandlerName] [nvarchar](255) NULL,
	[ReferenceCode] [nvarchar](255) NULL,
 CONSTRAINT [PK_Audit] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[DeadLetter]    Script Date: 4/4/2016 9:10:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeadLetter](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CreatedOn] [datetime] NOT NULL CONSTRAINT [DF_ErrorLog_CreatedOn]  DEFAULT (getdate()),
	[DateTime] [datetime] NOT NULL,
	[Server] [nvarchar](255) NULL,
	[VirtualHost] [nvarchar](255) NULL,
	[Exchange] [nvarchar](255) NOT NULL,
	[Properties] [nvarchar](max) NOT NULL,
	[Queue] [nvarchar](max) NOT NULL,
	[Topic] [nvarchar](255) NOT NULL,
	[Payload] [nvarchar](max) NOT NULL,
	[Error] [nvarchar](max) NOT NULL,
	[StackTrace] [nvarchar](max) NOT NULL,
	[CorrelationId] [uniqueidentifier] NOT NULL,
	[Type] [nvarchar](255) NOT NULL,
	[ConsumerTag] [nvarchar](255) NOT NULL,
	[RoutingKey] [nvarchar](255) NOT NULL,
	[Resend] [bit] NULL CONSTRAINT [DF_Errors_Resend]  DEFAULT ((0)),
	[Deleted] [bit] NULL CONSTRAINT [DF_Errors_Deleted]  DEFAULT ((0)),
	[RunId] [uniqueidentifier] NULL,
	[MessageId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_ErrorLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[KnownConsumers]    Script Date: 4/4/2016 9:10:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KnownConsumers](
	[Name] [nvarchar](255) NOT NULL,
 CONSTRAINT [PK_KnownConsumers] PRIMARY KEY CLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[TraceLog]    Script Date: 4/4/2016 9:10:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[TraceLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Logger] [varchar](100) NULL,
	[EventDateTime] [datetime] NULL,
	[EventLevel] [varchar](100) NULL,
	[UserName] [varchar](255) NULL,
	[ThreadName] [varchar](255) NULL,
	[EventMessage] [varchar](max) NULL,
	[Exception] [varchar](max) NULL,
	[RunId] [uniqueidentifier] NULL,
	[CorrelationId] [uniqueidentifier] NULL,
	[Handler] [varchar](255) NULL,
 CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
