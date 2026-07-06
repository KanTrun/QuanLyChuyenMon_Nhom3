# Phase 02 - Sidebar And Filters

## Overview
Priority: High
Status: Complete

Repair collapsed navigation and archive filtering lifecycle gaps.

## Related Files
- `wwwroot/css/admin-shell.css`
- `Components/Pages/Admin/ToChuc/DepartmentsPage.razor`
- `Components/Pages/Admin/ToChuc/GroupsPage.razor`
- `Components/Pages/Resource/ResourcePage.razor`
- `Components/Pages/Admin/DanhMucPage.razor`
- `Services/Admin/Sql/IMedDataStore.cs`
- `Services/Admin/Sql/MedDbDataStore.cs`
- `Services/Admin/Sql/MedDataStore.Identity.cs`

## Implementation
1. Display collapsed desktop group children as accessible flyouts.
2. Show archived nested departments even when active ancestors are excluded.
3. Prevent archived resource and technical-service edits from silently reactivating records.
4. Add active/archive/all filter and explicit archive lifecycle for groups.
5. Add behavioral tests for datastore lifecycle.

## Success Criteria
- Every authorized sidebar destination remains clickable after collapse.
- Archived department children, resources, services, and groups are visible under archive filters.
- Editing archived catalog records never silently restores them.
