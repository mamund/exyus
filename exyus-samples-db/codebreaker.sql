USE [exyus_samples]
GO
/****** Object:  Table [dbo].[CBGames]    Script Date: 03/30/2008 20:45:04 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CBGames](
  [id] [int] IDENTITY(1,1) NOT NULL,
  [userid] [nvarchar](50) NOT NULL,
  [datecreated] [datetime] NOT NULL CONSTRAINT [DF_CBGames_datecreated]  DEFAULT (getdate()),
  [maxattempts] [int] NOT NULL CONSTRAINT [DF_CBGames_maxattempts]  DEFAULT ((10)),
  [status] [nvarchar](50) NOT NULL CONSTRAINT [DF_CBGames_status]  DEFAULT (N'In-Progress'),
  [place1] [nvarchar](50) NOT NULL,
  [place2] [nvarchar](50) NOT NULL,
  [place3] [nvarchar](50) NOT NULL,
  [place4] [nvarchar](50) NOT NULL,
  [score] [money] NOT NULL CONSTRAINT [DF_CBGames_score]  DEFAULT ((0)),
 CONSTRAINT [PK_CBGames] PRIMARY KEY CLUSTERED 
(
  [id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CBAttempts]    Script Date: 03/30/2008 20:45:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CBAttempts](
  [id] [int] IDENTITY(1,1) NOT NULL,
  [gameid] [int] NOT NULL,
  [place1] [nvarchar](50) NOT NULL,
  [place2] [nvarchar](50) NOT NULL,
  [place3] [nvarchar](50) NOT NULL,
  [place4] [nvarchar](50) NOT NULL,
  [exact_match] [int] NOT NULL CONSTRAINT [DF_CBAttempts_exact_match]  DEFAULT ((0)),
  [near_hit] [int] NOT NULL CONSTRAINT [DF_CBAttempts_near_hit]  DEFAULT ((0)),
 CONSTRAINT [PK_CBAttempts] PRIMARY KEY CLUSTERED 
(
  [id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[cbgames_summary]    Script Date: 03/30/2008 20:44:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:    mca
-- Create date: 2008-03-29
-- Description: list summary of games
-- =============================================
CREATE PROCEDURE [dbo].[cbgames_summary]
AS
BEGIN
  select distinct
    cbg.userid as '@id',
    (select max(datecreated) from cbgames where userid=cbg.userid) as 'date-last-played',
    (select count(*) from cbgames where userid=cbg.userid) as 'games/total',
    (select count(*) from cbgames where userid=cbg.userid and status='In-Progress') as 'games/in-progress',
    (select count(*) from cbgames where userid=cbg.userid and status='Win') as 'games/wins',
    (select count(*) from cbgames where userid=cbg.userid and status='Loss') as 'games/losses',
    (select max(score) from cbgames where userid=cbg.userid) as 'games/high-score', 
    (select avg(score) from cbgames where userid=cbg.userid) as 'games/average-score'
  from cbgames cbg
  for xml path('summary'),root('summaries')
END
GO
/****** Object:  StoredProcedure [dbo].[cbattempts_list]    Script Date: 03/30/2008 20:44:55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:    mca
-- Create date: 2008-03-24
-- Description: get list of moves for a game
-- =============================================
CREATE PROCEDURE [dbo].[cbattempts_list]
  @userid nvarchar(50),
  @gameid int
AS
BEGIN
  SET NOCOUNT ON;

  -- valid game/userid
  if(select count(*) from cbgames where id=@gameid and userid=@userid)=0
    begin
      raiserror('game id not found [%i]',16,1,@gameid)
      return
    end
  --endif

  if(select count(*) from cbattempts where gameid=@gameid)=0
    begin
      raiserror('no records found (attempts) for this game [%1]',16,1,@gameid)
      return
    end
  else
    begin
      select 
        id as '@id',
        gameid  as '@game-id',
        @userid as '@user-id',
        place1,
        place2,
        place3,
        place4,
        exact_match as 'exact-matches',
        near_hit as 'near-hits'
      from cbattempts
      where gameid=@gameid
      for xml path('attempt'),root('attempts')
    end
  --endif
END
GO
/****** Object:  StoredProcedure [dbo].[cbattempts_read]    Script Date: 03/30/2008 20:44:55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:    mca
-- Create date: 2008-03-24
-- Description: read a single attempt row
-- =============================================
CREATE PROCEDURE [dbo].[cbattempts_read]
  @userid nvarchar(50),
  @gameid int,
  @id int
AS
BEGIN
  SET NOCOUNT ON;

  -- valid game/user
  if(select count(*) from cbgames where userid=@userid and id=@gameid)=0
    begin
      raiserror('game not found [%i]',16,1,@gameid)
      return
    end
  --endif

  if(select count(*) from cbattempts where id=@id)=0
    begin
      raiserror('attempt id not found [%i]',16,1,@id)
      return
    end
  else
    begin
      select 
        id as '@id',
        gameid as '@game-id',
        @userid as '@userid',
        (select name from cbusers where userid=@userid) as '@name',
        place1,
        place2,
        place3,
        place4,
        exact_match as 'exact-matches',
        near_hit as 'near-hits'
      from cbattempts
      where id=@id
      for xml path('attempt')
    end
  --endif
END
GO
/****** Object:  StoredProcedure [dbo].[cbgames_list]    Script Date: 03/30/2008 20:44:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:    mca
-- Create date: 2008-03-09
-- Description: get list of games for a user
-- =============================================
CREATE PROCEDURE [dbo].[cbgames_list]
  @userid nvarchar(50)
AS
BEGIN
  SET NOCOUNT ON;

  select 
    cbg.id as '@id',
    cbg.datecreated as 'date-created',
    cbg.maxattempts as 'max-attempts',
    cbg.status as 'status',
    cbg.score as 'score',
    cbg.place1 as 'code/place1',
    cbg.place2 as 'code/place2',
    cbg.place3 as 'code/place3',
    cbg.place4 as 'code/place4',
    (select 
      cba.id as '@id',
      cba.place1,
      cba.place2,
      cba.place3,
      cba.place4,
      cba.exact_match as 'exact-matches',
      cba.near_hit as 'near-hits' 
    from  cbattempts cba 
    where cbg.id=cba.gameid 
    order by cba.id
    for xml path('attempt'),  type) as 'attempts'
  from cbgames cbg
  where userid=@userid
  for xml path('game'), root('games')
END
GO
/****** Object:  StoredProcedure [dbo].[cbgames_read]    Script Date: 03/30/2008 20:44:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:    mca
-- Create date: 2008-03-24
-- Description: read inslge game record
-- =============================================
CREATE PROCEDURE [dbo].[cbgames_read]
  @userid nvarchar(50),
  @gameid int
AS
BEGIN
  if (select count(*) from cbgames where id=@gameid and userid=@userid)=0
    begin
      raiserror('game not found [%i]',16,1,@gameid)
      return
    end
  else
    begin
      select 
        cbg.id as '@id',
        cbg.datecreated as 'date-created',
        cbg.maxattempts as 'max-attempts',
        cbg.status as 'status',
        cbg.score as 'score',
        cbg.place1 as 'code/place1',
        cbg.place2 as 'code/place2',
        cbg.place3 as 'code/place3',
        cbg.place4 as 'code/place4',
        (select 
          cba.place1,
          cba.place2,
          cba.place3,
          cba.place4,
          cba.exact_match as 'exact-matches',
          cba.near_hit as 'near-hits' 
        from  cbattempts cba 
        where cbg.id=cba.gameid 
        order by cba.id
        for xml path('attempt'),  type) as 'attempts'
      from cbgames cbg
      where id=@gameid
      for xml path('game')
    end
  --endif
END
GO
/****** Object:  StoredProcedure [dbo].[cbattempts_add]    Script Date: 03/30/2008 20:44:55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:    mca
-- Create date: 2008-03-24
-- Description: add a new attempts row for the game
-- Note: this routine also computes and records scores
-- =============================================
CREATE PROCEDURE [dbo].[cbattempts_add]
  @userid nvarchar(50),
  @gameid int,
  @place1 nvarchar(10),
  @place2 nvarchar(10),
  @place3 nvarchar(10),
  @place4 nvarchar(10)
AS
BEGIN
  SET NOCOUNT ON;

  declare 
    @items nvarchar(10),
    @ans nvarchar(10),
    @t nvarchar(10),
    @r nvarchar(10),
    @l nvarchar(10),
    @p int,
    @r1 int,
    @r2 int,
    @score int,
    @past_attempts int,

    @st nvarchar(50),
    @a1 nvarchar(10),
    @a2 nvarchar(10),
    @a3 nvarchar(10),
    @a4 nvarchar(10),

    @h1 nvarchar(10),
    @h2 nvarchar(10),
    @h3 nvarchar(10),
    @h4 nvarchar(10),

    @max int

  -- make sure it's a valid game
  if(select count(*) from cbgames where id=@gameid and userid=@userid)=0
    begin
      raiserror('game id not found [%i]',16,1,@gameid)
      return
    end
  else
    begin
      -- get answers and current status
      select 
        @st=status,
        @a1=place1,
        @a2=place2,
        @a3=place3,
        @a4=place4
      from cbgames
      where id=@gameid

      -- if not in-progress, throw error
      if(@st!='In-Progress')
        begin
          raiserror('unable to add move - game status is [%s]',16,1,@st)
          return        
        end
      --endif
  
      -- get count of attempts
      select @past_attempts = count(*) from cbattempts where gameid=@gameid

      -- copy attempts for computing
      set @h1=@place1
      set @h2=@place2
      set @h3=@place3
      set @h4=@place4

      -- count exact matches
      set @r1=0
      if(@a1=@place1) 
        begin
          set @r1=@r1+1
          set @h1=''
          set @a1=''
        end
      --endif

      if(@a2=@place2) 
        begin
          set @r1=@r1+1
          set @h2=''
          set @a2=''
        end
      --endif

      if(@a3=@place3) 
        begin
          set @r1=@r1+1
          set @h3=''
          set @a3=''
        end
      --endif

      if(@a4=@place4) 
        begin
          set @r1=@r1+1
          set @h4=''
          set @a4=''
        end
      --endif

      -- check remaining items for near hits
      set @items = @h1+@h2+@h3+@h4
      set @ans = @a1+@a2+@a3+@a4
      set @t='' 
      set @p=0
      set @r2=0

      if(len(@items)!=0)
        begin
          set @t=substring(@items,1,1)
          set @p = charindex(@t,@ans)
          if(@p!=0)
            begin
              set @r2=@r2+1
              set @l  = substring(@ans,1,@p-1)
              set @r = substring(@ans,@p+1,len(@ans))
              set @ans = @l+@r
            end
          --endif
          set @items = substring(@items,2,len(@items)-1)
        end
      --endif

      if(len(@items)!=0)
        begin
          set @t=substring(@items,1,1)
          set @p = charindex(@t,@ans)
          if(@p!=0)
            begin
              set @r2=@r2+1
              set @l  = substring(@ans,1,@p-1)
              set @r = substring(@ans,@p+1,len(@ans))
              set @ans = @l+@r
            end
          --endif
          set @items = substring(@items,2,len(@items)-1)
        end
      --endif

      if(len(@items)!=0)
        begin
          set @t=substring(@items,1,1)
          set @p = charindex(@t,@ans)
          if(@p!=0)
            begin
              set @r2=@r2+1
              set @l  = substring(@ans,1,@p-1)
              set @r = substring(@ans,@p+1,len(@ans))
              set @ans = @l+@r
            end
          --endif
          set @items = substring(@items,2,len(@items)-1)
        end
      --endif

      if(len(@items)!=0)
        begin
          set @t=substring(@items,1,1)
          set @p = charindex(@t,@ans)
          if(@p!=0)
            begin
              set @r2=@r2+1
              set @l  = substring(@ans,1,@p-1)
              set @r = substring(@ans,@p+1,len(@ans))
              set @ans = @l+@r
            end
          --endif
          set @items = substring(@items,2,len(@items)-1)
        end
      --endif

      -- mark the winner and score
      if(@r1=4)
        begin
          set @score = (10-@past_attempts)*10 -- perfect score is ten
          update cbgames set status='Win', score=@score where id=@gameid
        end
      --endif

      -- mark the loser (if we maxed out attempts)
      if(@r1!=4)
        begin
          select @max=maxattempts from cbgames where @gameid=@gameid
          if(select count(*) from cbattempts where gameid=@gameid)=@max-1
            begin
              update cbgames set status='Loss', score=0 where id=@gameid
            end
          --endif
        end
      --endif

      -- ok now write the new row
      insert into cbattempts
        (gameid,place1,place2,place3,place4,exact_match,near_hit)
      values
        (@gameid,upper(@place1),upper(@place2),upper(@place3),upper(@place4),@r1,@r2)

      -- return new row for the client
      exec cbattempts_read @userid=@userid, @gameid=@gameid, @id=@@identity
    end
  --endif
END
GO
/****** Object:  StoredProcedure [dbo].[cbgames_add]    Script Date: 03/30/2008 20:44:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:    mca
-- Create date: 2008-03-24
-- Description: add new codebreaker game record
-- =============================================
CREATE PROCEDURE [dbo].[cbgames_add]
  @userid nvarchar(50),
  @maxattempts int = 10,
  @status nvarchar(50) = 'in-progress',
  @place1 nvarchar(50),
  @place2 nvarchar(50),
  @place3 nvarchar(50),
  @place4 nvarchar(50)
AS
BEGIN
  SET NOCOUNT ON;

  insert into cbgames
  (userid,maxattempts,status,place1,place2,place3,place4)
  values
  (@userid,@maxattempts,@status,@place1,@place2,@place3,@place4)

  exec cbgames_read @userid=@userid,@gameid=@@identity
END
GO
