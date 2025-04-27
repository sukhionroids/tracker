# LifeTrack - Goal Tracking App

LifeTrack is a Duolingo-inspired web application built with Blazor WebAssembly that helps you track your goals across different life categories, rewarding consistency and balance.

## Features

- **Category-Based Goal Tracking**: Organize goals into categories like Career, Education, Health, Finance, and Personal Development
- **Gamification System**: Earn XP points by completing goals with different difficulty levels
- **Consistency Tracking**: Maintain streaks for continued progress
- **Balance Bonus**: Get extra points for maintaining balance across all life areas
- **Progress Visualization**: Track your progress over time with visual indicators
- **Mobile-Friendly Design**: Works well on mobile devices
- **Progressive Web App**: Can be installed on mobile devices for offline use

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later

### Running the Application

1. Clone the repository
2. Navigate to the project directory
3. Run the following commands:

```bash
dotnet restore
dotnet run
```

4. Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## Deployment

This app can be easily deployed to Azure as a static web app:

1. Create an Azure Static Web App resource
2. Connect it to your GitHub repository
3. Configure the build settings to use .NET 8.0

## Technology Stack

- Blazor WebAssembly
- .NET 8.0
- Bootstrap 5
- Bootstrap Icons
- Progressive Web App features

## Future Enhancements

- Data persistence with Azure storage or local browser storage
- User authentication
- Social sharing and competition features
- Custom goal categories
- Goal reminders and notifications
- Weekly and monthly reports