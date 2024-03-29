USE [exyus_samples]
GO
/****** Object:  Table [dbo].[UGData]    Script Date: 03/15/2008 22:36:35 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UGData](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[firstname] [nvarchar](50) NOT NULL,
	[lastname] [nvarchar](50) NOT NULL,
	[birthdate] [datetime] NOT NULL,
	[experience] [nvarchar](25) NOT NULL,
 CONSTRAINT [PK_UGData] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[ugdata_list]    Script Date: 03/15/2008 22:36:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-02
-- Description:	return list from ugdata table
-- =============================================
CREATE PROCEDURE [dbo].[ugdata_list]
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from ugdata)=0
		begin
			raiserror('records not found',16,1)
			return
		end
	else
		begin
			select 
				id as '@id',
				firstname,
				lastname,
				birthdate,
				experience
			from ugdata
			order by id asc
			for xml path ('member'), root('member-list')
		end
	--endif
END
GO
/****** Object:  StoredProcedure [dbo].[ugdata_delete]    Script Date: 03/15/2008 22:36:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-02
-- Description:	delete a row from ugdata
-- =============================================
CREATE PROCEDURE [dbo].[ugdata_delete]
	@id int
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from ugdata where id=@id)=0
		begin
			raiserror('id not found [%i]',16,1,@id)
			return
		end
	else
		begin
			delete from ugdata where id=@id
		end
	--endif
END
GO
/****** Object:  StoredProcedure [dbo].[ugdata_read]    Script Date: 03/15/2008 22:36:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-01>
-- Description:	return single row from ugdata
-- =============================================
CREATE PROCEDURE [dbo].[ugdata_read] 
	@id int
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from ugdata where id=@id)=0
		begin
			raiserror('id not found [%i]',16,1,@id)
			return
		end
	else
		begin
			select
				id as '@id',
				firstname,
				lastname,
				birthdate,
				experience
			from ugdata
			where id=@id
			for xml path ('member')
		end
	--endif
END
GO
/****** Object:  StoredProcedure [dbo].[ugdata_update]    Script Date: 03/15/2008 22:36:33 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-02
-- Description:	update existing ugdata row
-- =============================================
CREATE PROCEDURE [dbo].[ugdata_update]
	@id int,
	@firstname nvarchar(50),
	@lastname nvarchar(50),
	@birthdate datetime,
	@experience nvarchar(25)
AS
BEGIN
	SET NOCOUNT ON;

	if(select count(*) from ugdata where id=@id)=0
		begin
			raiserror('id not found [%i]',16,1,@id)
		end
	else
		begin
			update ugdata set 
				firstname=@firstname,
				lastname=@lastname,
				birthdate=@birthdate,
				experience=@experience
			where id=@id
		end
	--endif

	exec ugdata_read @id

END
GO
/****** Object:  StoredProcedure [dbo].[ugdata_add]    Script Date: 03/15/2008 22:36:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		mca
-- Create date: 2008-03-02
-- Description:	add new ugdata row
-- =============================================
CREATE PROCEDURE [dbo].[ugdata_add]
	@firstname nvarchar(50),
	@lastname nvarchar(50),
	@birthdate datetime,
	@experience nvarchar(25)
AS
BEGIN
	SET NOCOUNT ON;

	-- create record
	insert into ugdata
		(firstname,lastname,birthdate,experience)
	values
		(@firstname,@lastname,@birthdate,@experience)

	-- return new record
	exec ugdata_read @id=@@identity
END
GO
