USE [IDP]
GO
SET IDENTITY_INSERT [dbo].[Permissions] ON 
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (1, NULL, 1, N'dashboard.view', N'Dashboard', N'/dashboard', N'fa-chart-line me-2', N'NavGroup', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (2, NULL, 2, N'applications.view', N'Applications', N'/applications', N'fa-key', N'NavGroup', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (4, NULL, 3, N'users.view', N'Users', N'/users', N'fa-users me-2', N'NavLink', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (8, NULL, 4, N'roles.view', N'Roles', N'/roles', N'fa-shield-alt me-2', N'NavLink', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (9, NULL, 5, N'tokens.view', N'Tokens', N'/tokens', N'fa-id-badge me-2', N'NavGroup', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (10, NULL, 6, N'activities.view', N'Activities', N'/activities', N'fa-clipboard-list me-2', N'NavGroup', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (11, 4, 7, N'users.add', N'Can Add', N'/users/adduser', NULL, N'Action', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (12, 4, 8, N'users.edit', N'Can Edit', N'/users/edituser', NULL, N'Action', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (13, 8, 9, N'roles.add', N'Can Add', N'/roles/addrole', NULL, N'Action', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (14, 8, 10, N'roles.edit', N'Can Edit', N'/roles/editrole', NULL, N'Action', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (15, 8, 11, N'roles.delete', N'Can Delete', N'/roles', NULL, N'Action', 1, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (18, NULL, 12, N'tenants.view', N'Tenants', N'/tenants', N'fa-building me-2', N'NavGroup', 0, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (22, 18, 13, N'tenants.add', N'Can Add', N'/tenants/addtenant', NULL, N'Action', 0, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (23, 18, 14, N'tenants.edit', N'Can Edit', N'/tenants/edittenant', NULL, N'Action', 0, NULL, 1, CAST(N'2024-08-23T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (27, NULL, 15, N'settings.view', N'Settings', N'/settings', N'fa-cog me-2', N'NavGroup', 0, NULL, 1, CAST(N'2024-08-25T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (28, 27, 16, N'settings.add', N'Can Add', N'/settings/addsetting', NULL, N'Action', 0, NULL, 1, CAST(N'2024-08-25T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (29, 27, 17, N'setttings.edit', N'Can Edit', N'/settings/editsetting', NULL, N'Action', 0, NULL, 1, CAST(N'2024-08-25T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (30, 27, 21, N'applications.add', N'Can Add', N'null', NULL, N'Action', 0, NULL, 1, CAST(N'2024-08-25T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (31, 27, 22, N'applications.edit', N'Can Edit', N'null', NULL, N'Action', 0, NULL, 1, CAST(N'2024-08-25T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (33, 27, 23, N'tokens.delete', N'tokens.delete', N'null', NULL, N'Action', 0, NULL, 1, CAST(N'2024-08-25T00:00:00.0000000' AS DateTime2))
GO
INSERT [dbo].[Permissions] ([Id], [ParentId], [Sequence], [Permissionkey], [PermissionName], [AccessUrl], [Icon], [ControlType], [IsEditable], [IsActive], [CreatedBy], [CreatedOn]) 
VALUES (35, 27, 25, N'CM_CanView', N'Can View', N'null', NULL, N'Action', 0, NULL, 1, CAST(N'2024-08-25T00:00:00.0000000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[Permissions] OFF
GO
