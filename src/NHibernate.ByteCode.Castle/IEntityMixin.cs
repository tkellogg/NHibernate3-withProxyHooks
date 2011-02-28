using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHibernate.ByteCode.Castle
{
    /// <summary>
    /// Mixins may implement this interface if they want to be initialized with the target object
    /// </summary>
    public interface IEntityMixin
    {
        /// <summary>
        /// Initialize a mixin instance with the entity object that it's being attached to
        /// </summary>
        /// <remarks>The reason for the bizaar name is because the entity will inadvertently
        /// implement this interface, so we need to make some effort to avoid name collision.</remarks>
        /// <param name="entity"></param>
        void NHibernateMixin_Initialize(object entity);

        /// <summary>
        /// Indicates that the mixin has been satisfactorly initialized. Initialization will 
        /// continue to occur if this is false.
        /// </summary>
        /// <remarks>The reason for the bizaar name is because the entity will inadvertently
        /// implement this interface, so we need to make some effort to avoid name collision.</remarks>
        /// <param name="entity"></param>
        bool NHibernateMixin_IsInitialized { get; }
    }
}
