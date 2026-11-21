namespace Acme.Bakery.Platform.Shared.Domain.Model.Entities;

// "Etiqueta" de auditoría: las entidades que la implementan reciben created_at / updated_at
// automáticamente gracias al AuditableEntityInterceptor al momento de guardar.
public interface IAuditableEntity
{
    // Fecha y hora en que se creó el registro (la llena el interceptor)
    DateTime? CreatedAt { get; set; }

    // Fecha y hora de la última modificación (la llena el interceptor)
    DateTime? UpdatedAt { get; set; }
}
