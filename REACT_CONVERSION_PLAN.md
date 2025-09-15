# React Conversion Plan for Finaid Application

## Current Architecture Analysis
**Current Stack**: ASP.NET Core 8.0 Blazor Server
**Frontend**: Razor Components (.razor files)
**Backend**: .NET 8.0 Web API with Entity Framework
**Key Features**: FAFSA forms, document management, AI chat, dashboard, OCR processing

## Phase 1: Foundation Setup

### 1.1 React Project Structure
```
finaid-react/
├── public/
│   ├── index.html
│   └── favicon.png
├── src/
│   ├── components/
│   │   ├── common/
│   │   ├── forms/
│   │   ├── dashboard/
│   │   ├── documents/
│   │   └── chat/
│   ├── pages/
│   ├── services/
│   ├── hooks/
│   ├── utils/
│   ├── types/
│   └── styles/
├── package.json
└── tsconfig.json
```

### 1.2 Technology Stack
- **React 18** with TypeScript
- **Vite** for build tooling (faster than Create React App)
- **React Router** for navigation
- **TanStack Query (React Query)** for API state management
- **Zod** for validation (replacing FluentValidation)
- **Tailwind CSS** for styling (replacing Bootstrap)
- **Axios** for HTTP client
- **SignalR Client** for real-time updates

### 1.3 Package.json Setup
```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.8.0",
    "@tanstack/react-query": "^4.28.0",
    "axios": "^1.3.0",
    "zod": "^3.20.0",
    "@microsoft/signalr": "^7.0.0",
    "tailwindcss": "^3.2.0"
  },
  "devDependencies": {
    "@types/react": "^18.0.0",
    "@types/react-dom": "^18.0.0",
    "typescript": "^4.9.0",
    "vite": "^4.1.0",
    "@vitejs/plugin-react": "^3.1.0"
  }
}
```

## Phase 2: Component Migration Strategy

### 2.1 Component Mapping
| Blazor Component | React Equivalent | Priority |
|------------------|------------------|----------|
| MainLayout.razor | App.tsx + Layout components | High |
| NavMenu.razor | Navigation component | High |
| Dashboard.razor | Dashboard page | High |
| FAFSA.razor | FAFSA form page | High |
| Documents.razor | Documents page | High |
| ChatInterface.razor | Chat component | Medium |
| Progress.razor | Progress tracking | Medium |

### 2.2 Component Conversion Patterns

**Blazor to React Conversion Examples:**

**Blazor Component:**
```razor
@page "/dashboard"
<DashboardLayout>
    <ProgressBar Progress="@currentProgress" />
    <QuickActions />
</DashboardLayout>

@code {
    private decimal currentProgress = 0.65m;
}
```

**React Equivalent:**
```tsx
// Dashboard.tsx
const Dashboard: React.FC = () => {
    const { data: progress } = useQuery(['progress'], fetchProgress);
    
    return (
        <DashboardLayout>
            <ProgressBar progress={progress?.currentProgress || 0} />
            <QuickActions />
        </DashboardLayout>
    );
};
```

## Phase 3: Backend API Adaptation

### 3.1 API Layer Changes
- Keep existing .NET Web API controllers
- Add CORS configuration for React frontend
- Convert Blazor component parameters to RESTful endpoints
- Maintain existing service layer and business logic

### 3.2 SignalR Integration
```tsx
// useSignalR hook
const useSignalR = (hubUrl: string) => {
    const [connection, setConnection] = useState<HubConnection>();
    
    useEffect(() => {
        const newConnection = new HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();
            
        setConnection(newConnection);
        
        return () => {
            newConnection.stop();
        };
    }, [hubUrl]);
    
    return connection;
};
```

## Phase 4: State Management

### 4.1 React Query for Server State
```tsx
// API service hooks
const useFAFSAData = (applicationId: string) => {
    return useQuery(['fafsa', applicationId], () => 
        axios.get(`/api/fafsa/${applicationId}`)
    );
};

const useSubmitFAFSA = () => {
    return useMutation((data: FAFSAData) => 
        axios.post('/api/fafsa/submit', data)
    );
};
```

### 4.2 Context API for UI State
```tsx
// AppStateContext.tsx
const AppStateContext = createContext<AppState>({});

export const AppStateProvider: React.FC = ({ children }) => {
    const [currentStep, setCurrentStep] = useState(0);
    const [userProfile, setUserProfile] = useState<UserProfile>();
    
    return (
        <AppStateContext.Provider value={{ currentStep, setCurrentStep, userProfile, setUserProfile }}>
            {children}
        </AppStateContext.Provider>
    );
};
```

## Phase 5: Form Handling

### 5.1 React Hook Form Integration
```tsx
// FAFSAForm.tsx
const FAFSAForm: React.FC = () => {
    const { register, handleSubmit, formState: { errors } } = useForm<FAFSAData>();
    const { mutate: submitFAFSA, isLoading } = useSubmitFAFSA();
    
    const onSubmit = (data: FAFSAData) => {
        submitFAFSA(data);
    };
    
    return (
        <form onSubmit={handleSubmit(onSubmit)}>
            <input {...register('firstName', { required: true })} />
            {errors.firstName && <span>First name is required</span>}
            <button type="submit" disabled={isLoading}>
                {isLoading ? 'Submitting...' : 'Submit'}
            </button>
        </form>
    );
};
```

### 5.2 Zod Validation Schema
```tsx
// fafsaSchema.ts
const fafsaSchema = z.object({
    firstName: z.string().min(1, 'First name is required'),
    lastName: z.string().min(1, 'Last name is required'),
    ssn: z.string().regex(/^\d{3}-\d{2}-\d{4}$/, 'Invalid SSN format'),
    // ... other fields matching .NET models
});

type FAFSAData = z.infer<typeof fafsaSchema>;
```

## Phase 6: Styling and UI

### 6.1 Tailwind CSS Configuration
```javascript
// tailwind.config.js
module.exports = {
    content: ['./src/**/*.{js,jsx,ts,tsx}'],
    theme: {
        extend: {
            colors: {
                primary: '#2563eb',
                secondary: '#64748b',
                // Match existing color scheme
            }
        }
    }
};
```

### 6.2 Component Styling Pattern
```tsx
// DashboardCard.tsx
const DashboardCard: React.FC = ({ title, children }) => {
    return (
        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">
                {title}
            </h3>
            {children}
        </div>
    );
};
```

## Phase 7: Testing Strategy

### 7.1 Testing Setup
- **Jest** + **React Testing Library** for unit tests
- **Playwright** for E2E tests (reuse existing tests)
- **MSW** for API mocking

### 7.2 Test Conversion
```tsx
// Dashboard.test.tsx
test('renders progress bar with correct percentage', async () => {
    server.use(
        rest.get('/api/progress', (req, res, ctx) => {
            return res(ctx.json({ currentProgress: 0.65 }));
        })
    );
    
    render(<Dashboard />);
    
    await waitFor(() => {
        expect(screen.getByText('65%')).toBeInTheDocument();
    });
});
```

## Phase 8: Deployment and CI/CD

### 8.1 Build Configuration
```javascript
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    server: {
        proxy: {
            '/api': 'http://localhost:5033',
            '/hubs': 'http://localhost:5033'
        }
    },
    build: {
        outDir: 'dist',
        sourcemap: true
    }
});
```

### 8.2 Docker Configuration
```dockerfile
# React frontend Dockerfile
FROM node:18-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
RUN npm run build
EXPOSE 3000
CMD ["npm", "run", "preview"]
```

## Phase 9: Migration Timeline

### Week 1-2: Foundation Setup
- ✅ Create React project with TypeScript
- ✅ Configure build tools and dependencies
- ✅ Set up basic routing structure
- ✅ Create shared component library

### Week 3-4: Core Pages Migration
- ✅ Convert MainLayout and navigation
- ✅ Migrate Dashboard components
- ✅ Implement basic form components
- ✅ Set up API service layer

### Week 5-6: Complex Features
- ✅ FAFSA form with validation
- ✅ Document upload components
- ✅ Chat interface with SignalR
- ✅ Progress tracking system

### Week 7-8: Polish and Testing
- ✅ Responsive design implementation
- ✅ Comprehensive testing suite
- ✅ Performance optimization
- ✅ Deployment configuration

## Phase 10: Risk Mitigation

### 10.1 Technical Risks
- **SignalR integration complexity** - Use established @microsoft/signalr package
- **Form state management** - Implement React Hook Form with Zod validation
- **CSS migration** - Use Tailwind CSS utility classes for consistency

### 10.2 Organizational Risks
- **Team learning curve** - Provide React training sessions
- **Parallel development** - Maintain both Blazor and React during transition
- **Testing coverage** - Implement comprehensive test suite from start

## Success Metrics

- ✅ 100% feature parity with Blazor version
- ✅ Improved performance (faster load times)
- ✅ Better developer experience (hot reload, TypeScript)
- ✅ Maintained test coverage (>80%)
- ✅ Successful production deployment

## Next Steps

1. **Initialize React project** with recommended stack
2. **Create component mapping document** for each Blazor component
3. **Set up API proxy** for development
4. **Begin with layout components** (MainLayout, Navigation)
5. **Incremental migration** page by page
6. **Parallel testing** to ensure functionality
7. **Performance benchmarking** at each stage

This plan provides a structured approach to migrating from Blazor Server to React while maintaining all existing functionality and ensuring a smooth transition.