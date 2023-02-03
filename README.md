mkdir sqlite_app  
cd sqlite_app  
dotnet new console  
dotnet add package Microsoft.EntityFrameworkCore.Sqlite  
  
Добавить в проект  
<ItemGroup>  
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="3.1.0" />  
</ItemGroup>  
  
dotnet restore  