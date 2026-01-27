// C:\Projects\RotaAppMobile\src\screens\notifications\NotificationsScreen.tsx

import React, { useState, useEffect } from 'react';
import { View, ScrollView, StyleSheet, RefreshControl, TouchableOpacity, Alert } from 'react-native';
import { Card, Text, List, IconButton, Button, Chip } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { formatDistanceToNow } from 'date-fns';
import { tr } from 'date-fns/locale';
import notificationService, { Notification, NotificationType } from '../../services/notificationService';
import signalRService from '../../services/signalRService';

const getNotificationIcon = (type: NotificationType): string => {
  switch (type) {
    case NotificationType.JOURNEY_ASSIGNED:
      return 'truck-plus';
    case NotificationType.JOURNEY_STATUS_CHANGED:
      return 'truck-check';
    case NotificationType.ROUTE_CANCELLED:
      return 'truck-remove';
    case NotificationType.SYSTEM_ANNOUNCEMENT:
      return 'bullhorn';
    case NotificationType.DEADLINE_REMINDER:
      return 'clock-alert';
    case NotificationType.PERFORMANCE_ALERT:
      return 'speedometer';
    case NotificationType.MAINTENANCE_REMINDER:
      return 'wrench';
    case NotificationType.CUSTOMER_FEEDBACK:
      return 'comment-text';
    default:
      return 'bell';
  }
};

const getNotificationColor = (type: NotificationType) => {
  switch (type) {
    case NotificationType.JOURNEY_ASSIGNED:
      return { color: '#2196F3', background: '#E3F2FD' };
    case NotificationType.JOURNEY_STATUS_CHANGED:
      return { color: '#4CAF50', background: '#E8F5E8' };
    case NotificationType.ROUTE_CANCELLED:
      return { color: '#F44336', background: '#FFEBEE' };
    case NotificationType.SYSTEM_ANNOUNCEMENT:
      return { color: '#FF9800', background: '#FFF3E0' };
    case NotificationType.DEADLINE_REMINDER:
      return { color: '#FF5722', background: '#FFF3E0' };
    case NotificationType.PERFORMANCE_ALERT:
      return { color: '#9C27B0', background: '#F3E5F5' };
    case NotificationType.MAINTENANCE_REMINDER:
      return { color: '#607D8B', background: '#F5F5F5' };
    case NotificationType.CUSTOMER_FEEDBACK:
      return { color: '#00BCD4', background: '#E0F2F1' };
    default:
      return { color: '#9E9E9E', background: '#F5F5F5' };
  }
};

const NotificationsScreen: React.FC = () => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [refreshing, setRefreshing] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadNotifications();
    
    // Connect to SignalR for real-time notifications
    signalRService.connect();
    
    const unsubscribe = signalRService.onNotification((notification) => {
      // Add new notification to the list
      setNotifications(prev => [notification as Notification, ...prev]);
    });

    return () => {
      unsubscribe();
      signalRService.disconnect();
    };
  }, []);

  const loadNotifications = async () => {
    try {
      setLoading(true);
      const response = await notificationService.getNotifications({
        page: 1,
        pageSize: 50
      });
      setNotifications(response.notifications);
    } catch (error) {
      console.error('Bildirimler yüklenemedi:', error);
      Alert.alert('Hata', 'Bildirimler yüklenirken bir hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  const onRefresh = React.useCallback(() => {
    setRefreshing(true);
    loadNotifications().finally(() => setRefreshing(false));
  }, []);

  const markAsRead = async (id: string) => {
    try {
      await notificationService.markAsRead(id);
      const updated = notifications.map(n => 
        n.id === id ? { ...n, isRead: true } : n
      );
      setNotifications(updated);
    } catch (error) {
      console.error('Bildirim okundu olarak işaretlenemedi:', error);
      Alert.alert('Hata', 'Bildirim işaretlenirken bir hata oluştu.');
    }
  };

  const markAllAsRead = async () => {
    try {
      await notificationService.markAllAsRead();
      const updated = notifications.map(n => ({ ...n, isRead: true }));
      setNotifications(updated);
    } catch (error) {
      console.error('Bildirimler okundu olarak işaretlenemedi:', error);
      Alert.alert('Hata', 'Bildirimler işaretlenirken bir hata oluştu.');
    }
  };

  const deleteNotification = async (id: string) => {
    try {
      await notificationService.deleteNotification(id);
      const updated = notifications.filter(n => n.id !== id);
      setNotifications(updated);
    } catch (error) {
      console.error('Bildirim silinemedi:', error);
      Alert.alert('Hata', 'Bildirim silinirken bir hata oluştu.');
    }
  };

  const clearAll = () => {
    Alert.alert(
      'Okunmuşları Temizle',
      'Okunmuş tüm bildirimler silinecek. Emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        { 
          text: 'Sil', 
          style: 'destructive',
          onPress: async () => {
            try {
              await notificationService.clearReadNotifications();
              await loadNotifications();
            } catch (error) {
              console.error('Okunmuş bildirimler temizlenemedi:', error);
              Alert.alert('Hata', 'Bildirimler temizlenirken bir hata oluştu.');
            }
          }
        }
      ]
    );
  };

  const unreadCount = notifications.filter(n => !n.isRead).length;

  return (
    <View style={styles.container}>
      {/* Header */}
      <Card style={styles.headerCard}>
        <Card.Content>
          <View style={styles.headerContent}>
            <View>
              <Text variant="titleLarge">Bildirimler</Text>
              <Text variant="bodyMedium" style={styles.subtitle}>
                {unreadCount > 0 ? `${unreadCount} okunmamış` : 'Tümü okundu'}
              </Text>
            </View>
            {unreadCount > 0 && (
              <Button mode="text" onPress={markAllAsRead}>
                Tümünü Oku
              </Button>
            )}
          </View>
        </Card.Content>
      </Card>

      {/* Liste */}
      <ScrollView
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
        style={styles.scrollView}
      >
        {notifications.length === 0 ? (
          <Card style={styles.emptyCard}>
            <Card.Content style={styles.emptyContent}>
              <Icon name="bell-off-outline" size={48} color="#ccc" />
              <Text variant="bodyLarge" style={styles.emptyText}>
                Henüz bildirim yok
              </Text>
            </Card.Content>
          </Card>
        ) : (
          <>
            {notifications.map((notification) => (
              <TouchableOpacity
                key={notification.id}
                onPress={() => markAsRead(notification.id)}
                activeOpacity={0.7}
              >
                <Card style={[
                  styles.notificationCard,
                  !notification.isRead && styles.unreadCard
                ]}>
                  <Card.Content>
                    <View style={styles.notificationContent}>
                      <View style={[
                        styles.iconContainer,
                        { backgroundColor: getNotificationColor(notification.type).background }
                      ]}>
                        <Icon 
                          name={getNotificationIcon(notification.type)} 
                          size={24} 
                          color={getNotificationColor(notification.type).color}
                        />
                      </View>
                      
                      <View style={styles.textContainer}>
                        <Text 
                          variant="titleMedium" 
                          style={!notification.isRead ? styles.unreadTitle : undefined}
                        >
                          {notification.title}
                        </Text>
                        <Text variant="bodyMedium" style={styles.message}>
                          {notification.message}
                        </Text>
                        <Text variant="bodySmall" style={styles.timestamp}>
                          {formatDistanceToNow(new Date(notification.createdAt), { 
                            addSuffix: true, 
                            locale: tr 
                          })}
                        </Text>
                      </View>

                      <IconButton
                        icon="close"
                        size={20}
                        onPress={() => deleteNotification(notification.id)}
                      />
                    </View>
                  </Card.Content>
                </Card>
              </TouchableOpacity>
            ))}

            {notifications.length > 0 && (
              <Button
                mode="text"
                onPress={clearAll}
                style={styles.clearButton}
                textColor="#EF4444"
              >
                Okunmuşları Temizle
              </Button>
            )}
          </>
        )}
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  headerCard: {
    margin: 10,
  },
  headerContent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  subtitle: {
    color: '#666',
    marginTop: 4,
  },
  scrollView: {
    flex: 1,
  },
  notificationCard: {
    marginHorizontal: 10,
    marginVertical: 5,
  },
  unreadCard: {
    borderLeftWidth: 3,
    borderLeftColor: '#3B82F6',
  },
  notificationContent: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  textContainer: {
    flex: 1,
  },
  unreadTitle: {
    fontWeight: 'bold',
  },
  message: {
    color: '#666',
    marginTop: 4,
  },
  timestamp: {
    color: '#999',
    marginTop: 4,
  },
  emptyCard: {
    margin: 20,
  },
  emptyContent: {
    alignItems: 'center',
    paddingVertical: 40,
  },
  emptyText: {
    marginTop: 16,
    color: '#999',
  },
  clearButton: {
    margin: 20,
  },
});

export default NotificationsScreen;