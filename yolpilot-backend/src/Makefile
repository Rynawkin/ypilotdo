mig:
	dotnet ef --startup-project Monolith.WebAPI/ migrations add $(name) && cd ..
migrm:
	dotnet ef migrations remove --startup-project Monolith.WebAPI/ && cd ..
dbup:
	dotnet ef --startup-project Monolith.WebAPI/ database update && cd ..
r:
	cd ./MVC/ && dotnet run && cd ..
wr:
	cd ./MVC/ && dotnet watch run && cd ..
res:
	cd ./MVC/ && dotnet restore && cd ..