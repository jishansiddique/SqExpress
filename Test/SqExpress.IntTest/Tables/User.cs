﻿using System;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Tables
{
    public class User : TableBase
    {
        public Int32TableColumn UserId { get; }
        public GuidTableColumn ExternalId { get; }
        public StringTableColumn FirstName { get; }
        public StringTableColumn LastName { get; }
        public StringTableColumn Email { get; }
        public Int32TableColumn Version { get; }
        public DateTimeTableColumn Created { get; }
        public DateTimeTableColumn Modified { get; }
        public DateTimeTableColumn RegDate { get; }

        public User() : this(default) { }

        public User(Alias alias = default) : base("public", "ItUser", alias)
        {
            //Columns
            this.UserId = this.CreateInt32Column("UserId", ColumnMeta.PrimaryKey().Identity());
            this.ExternalId = this.CreateGuidColumn("ExternalId");
            this.FirstName = this.CreateStringColumn("FirstName", 255);
            this.LastName = this.CreateStringColumn("LastName", 255);
            this.Email = this.CreateStringColumn("Email", 255);
            this.RegDate = this.CreateDateTimeColumn("RegDate");
            this.Version = this.CreateInt32Column("Version", ColumnMeta.DefaultValue(0));
            this.Created = this.CreateDateTimeColumn("Created", columnMeta: ColumnMeta.DefaultValue(GetUtcDate()));
            this.Modified = this.CreateDateTimeColumn("Modified", columnMeta: ColumnMeta.DefaultValue(GetUtcDate()));

            //Indexes
            this.AddUniqueClusteredIndex("IX_ItUser_ExternalId_CustomName", this.ExternalId);
            this.AddIndex(IndexMetaColumn.Asc(this.FirstName));
            this.AddIndex(IndexMetaColumn.Desc(this.LastName));
        }
    }
}