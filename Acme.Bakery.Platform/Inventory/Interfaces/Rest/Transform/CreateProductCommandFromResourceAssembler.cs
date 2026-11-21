using Acme.Bakery.Platform.Inventory.Domain.Model.Commands;
using Acme.Bakery.Platform.Inventory.Interfaces.Rest.Resources;

namespace Acme.Bakery.Platform.Inventory.Interfaces.Rest.Transform;

// Traductor: JSON de entrada (Resource) → orden de negocio (Command)
public static class CreateProductCommandFromResourceAssembler
{
    // Convierte el CreateProductResource recibido en un CreateProductCommand
    public static CreateProductCommand ToCommandFromResource(CreateProductResource resource)
    {
        return new CreateProductCommand(resource.Name, resource.Price, resource.Stock, resource.Category);
    }
}
