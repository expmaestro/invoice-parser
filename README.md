# Logistics Invoice Parser

This project is a web application that allows users to upload logistic invoice photos, parse them using AI, and create a simple object representing the parsed data.

## Project Structure

The project is divided into two main parts: the client and the server.

### Client

The client is built using Angular and is responsible for the user interface. It includes:

- **Components**: 
  - `invoice-upload`: A component for uploading invoice photos.
  - `invoice-details`: A component for displaying parsed invoice details.

- **Models**: 
  - `invoice.model.ts`: Defines the structure of the invoice data.

- **Services**: 
  - `invoice.service.ts`: Manages invoice logic, including uploading and retrieving data.
  - `ai.service.ts`: Interacts with the AI service to parse invoice data.

- **Main Application Files**: 
  - `app.component.ts`: The root component of the application.
  - `app.module.ts`: The main module that declares and imports components and services.
  - `app-routing.module.ts`: Sets up routing for the application.

### Server

The server is built using C# .NET and handles the backend logic. It includes:

- **Controllers**: 
  - `InvoiceController.cs`: Handles HTTP requests related to invoices.

- **Models**: 
  - `Invoice.cs`: Represents the structure of the invoice data on the server side.

- **Services**: 
  - `IAiService.cs`: Defines the contract for AI services.
  - `IInvoiceService.cs`: Defines the contract for invoice services.
  - `OpenAiService.cs`: Implements the AI parsing logic.
  - `InvoiceService.cs`: Manages invoices on the server side.

- **Program Entry Point**: 
  - `Program.cs`: The entry point of the C# .NET application.

## Getting Started

To get started with the project, clone the repository and install the necessary dependencies for both the client and server.

### Client

1. Navigate to the `client` directory.
2. Run `npm install` to install the Angular dependencies.
3. Run `ng serve` to start the Angular development server.

### Server

1. Navigate to the `server` directory.
2. Run `dotnet restore` to restore the .NET dependencies.
3. Run `dotnet run` to start the C# .NET application.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License.