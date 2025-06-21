# Use the official .NET SDK image to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY RentalService.csproj ./
RUN dotnet restore RentalService.csproj

# Copy the rest of the source code
COPY . .

# Build and publish the app
RUN dotnet publish RentalService.csproj -c Release -o /app/publish --no-restore

# Use the official ASP.NET runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Cài đặt công cụ kiểm tra DNS và curl
RUN apt-get update && apt-get install -y dnsutils curl

# Kiểm tra DNS và outbound tới api-merchant.payos.vn khi build
RUN nslookup api-merchant.payos.vn || true
RUN curl -Iv https://api-merchant.payos.vn || true

# (Tùy chọn) Override DNS nếu cần, bỏ comment dòng sau nếu gặp lỗi DNS
# RUN echo "nameserver 8.8.8.8" > /etc/resolv.conf

# Expose port 80
EXPOSE 80

# Set the entrypoint
ENTRYPOINT ["dotnet", "RentalService.dll"]
