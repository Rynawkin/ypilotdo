import React from 'react';
import { SafeAreaView, StatusBar, Platform, View, StyleSheet, ViewStyle } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

interface SafeScreenProps {
  children: React.ReactNode;
  style?: ViewStyle;
  backgroundColor?: string;
}

export const SafeScreen: React.FC<SafeScreenProps> = ({ 
  children, 
  style, 
  backgroundColor = '#fff' 
}) => {
  const insets = useSafeAreaInsets();
  
  return (
    <SafeAreaView style={[styles.container, { backgroundColor }]}>
      <View style={[
        styles.content,
        { 
          paddingTop: Platform.OS === 'android' ? StatusBar.currentHeight : 0,
          paddingBottom: insets.bottom,
        },
        style
      ]}>
        {children}
      </View>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  content: {
    flex: 1,
  },
});