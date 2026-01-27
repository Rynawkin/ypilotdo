// C:\Projects\RotaAppMobile\App.tsx

import 'text-encoding-polyfill';
import React from 'react';
import {StatusBar, View, Text, Button} from 'react-native';
import {SafeAreaProvider} from 'react-native-safe-area-context';
import {Provider as PaperProvider} from 'react-native-paper';
import {AuthProvider} from './src/contexts/AuthContext';
import RootNavigator from './src/navigation/RootNavigator';

// Error Boundary Component
interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

interface ErrorBoundaryProps {
  children: React.ReactNode;
}

class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = {hasError: false, error: null};
  }

  static getDerivedStateFromError(error: Error) {
    return {hasError: true, error};
  }

  componentDidCatch(error: Error, errorInfo: any) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
  }

  resetError = () => {
    this.setState({hasError: false, error: null});
  };

  render() {
    if (this.state.hasError) {
      return (
        <View style={{flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20}}>
          <Text style={{fontSize: 18, fontWeight: 'bold', marginBottom: 10}}>
            Bir Hata Oluştu
          </Text>
          <Text style={{fontSize: 14, color: '#666', textAlign: 'center', marginBottom: 20}}>
            {this.state.error?.message || 'Beklenmeyen bir hata oluştu.'}
          </Text>
          <Button title="Uygulamayı Yeniden Başlat" onPress={this.resetError} />
        </View>
      );
    }

    return this.props.children;
  }
}

function App(): React.JSX.Element {
  return (
    <ErrorBoundary>
      <SafeAreaProvider>
        <StatusBar barStyle="dark-content" backgroundColor="#ffffff" />
        <PaperProvider>
          <AuthProvider>
            <RootNavigator />
          </AuthProvider>
        </PaperProvider>
      </SafeAreaProvider>
    </ErrorBoundary>
  );
}

export default App;