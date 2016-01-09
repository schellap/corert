using Internal.TypeSystem;
using Internal.IL;
using Internal.IL.Stubs;

namespace ILCompiler.Compiler.IL.Stubs
{
    public abstract class BootstrapMethod : ILStubMethod
    {
        protected MethodSignature _signature;
        protected DefType _owningType;

        public BootstrapMethod(DefType owningType)
        {
            _owningType = owningType;
        }


        public override TypeSystemContext Context
        {
            get
            {
                return OwningType.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    _signature = BootstrapMethodSignature();
                }

                return _signature;
            }
        }

        public abstract override string Name { get; }
        protected abstract MethodSignature BootstrapMethodSignature();
    }

}
