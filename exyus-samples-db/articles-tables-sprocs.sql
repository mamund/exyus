USE [exyus_samples]
GO
/****** Object:  Table [dbo].[articles]    Script Date: 03/15/2008 22:35:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[articles](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[datecreated] [datetime] NOT NULL CONSTRAINT [DF_articles_datecreated]  DEFAULT (getdate()),
	[status] [nvarchar](50) NOT NULL CONSTRAINT [DF_articles_status]  DEFAULT (N'published'),
	[title] [nvarchar](100) NOT NULL,
	[body] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_articles] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[articles_list]    Script Date: 03/15/2008 22:35:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-11
-- Description:	list articles
-- =============================================
CREATE PROCEDURE [dbo].[articles_list]
	@status nvarchar(50) = null
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from articles)=0
		begin
			raiserror('records not found',16,1)
			return
		end
	else
		begin
			select id as '@id',
				[status] as 'status',
				datecreated as 'date-created',
				title,
				body
			from articles
			where
				@status is null or @status=[status]
			order by datecreated desc
			for xml path('article'), root('articles')
		end
	--endif
END
GO
/****** Object:  StoredProcedure [dbo].[articles_read]    Script Date: 03/15/2008 22:35:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-11
-- Description:	return a single article
-- =============================================
CREATE PROCEDURE [dbo].[articles_read] 
	@id int
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from articles where id=@id)=0
		begin
			raiserror('id not found [%i]',16,1,@id)
			return
		end
	else
		begin
			select 
				id as '@id',
				[status] as 'status',
				datecreated as 'date-created',
				title,
				body
			from articles
			where id=@id
			for xml path('article')
		end
	--endif
END
GO
/****** Object:  StoredProcedure [dbo].[articles_delete]    Script Date: 03/15/2008 22:35:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-11
-- Description:	delete an existing article
-- =============================================
CREATE PROCEDURE [dbo].[articles_delete]
	@id int
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from articles where id=@id)=0
		begin
			raiserror('id not found [%i]',16,1,@id)
			return
		end
	else
		begin
			delete from articles where id=@id
		end
	--endif
END
GO
/****** Object:  StoredProcedure [dbo].[articles_update]    Script Date: 03/15/2008 22:35:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-11
-- Description:	update existing article
-- =============================================
CREATE PROCEDURE [dbo].[articles_update]
	@id int,
	@title nvarchar(100),
	@body nvarchar(max),
	@status nvarchar(50)
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from articles where id=@id)=0
		begin
			raiserror('id not found [%i]',16,1,@id)
			return
		end
	else
		begin
			update articles set
				title=@title,
				body=@body,
				[status]=@status
			where
				id=@id

			exec articles_read @id
		end
	--endif
END
GO
/****** Object:  StoredProcedure [dbo].[articles_update_status]    Script Date: 03/15/2008 22:35:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-11
-- Description:	update status of article
-- =============================================
CREATE PROCEDURE [dbo].[articles_update_status]
	@id int,
	@status nvarchar(50)
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from articles where id=@id)=0
		begin
			raiserror('id not found [%i]',16,1,@id)
			return
		end
	else
		begin
			update articles 
			set [status]=@status
			where id=@id

			exec articles_read @id
		end
	--endif

END
GO
/****** Object:  StoredProcedure [dbo].[articles_add]    Script Date: 03/15/2008 22:35:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-11
-- Description:	add a new article
-- =============================================
CREATE PROCEDURE [dbo].[articles_add]
	@title nvarchar(100),
	@body nvarchar(max),
	@status nvarchar(50) = 'published'
AS
BEGIN
	SET NOCOUNT ON;

	insert into articles
		(status,title,body)
	values
		(@status,@title,@body)

	-- return new record
	exec articles_read @@identity

END
GO
