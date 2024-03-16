#include <stdio.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <arpa/inet.h>
#include <sys/epoll.h>
#include <fcntl.h>
#include <pthread.h>

#define PORT 10035
#define BUFFER_SIZE 1024
#define MAX_EVENTS 30
#define RESOURCE_PATH "/hello"
#define MAX_CONN_QUEUE 10
#define MAX_PLAYERS 10
#define NUM_ZOMBIES 10

void epoll_events(int lissock_fd);
// Handle new connection. On success, it returns the new connection socket's fd.
int hdnc(int lissock_fd, int epoll_fd);
// Handle new data. Implements a simple GET request at RESOURCE_PATH
int hdnd(int sock_fd);
// Handle role selection data.
void hdroleselection(int sock_fd, int msg_len);
void hdmovements(int sock_fd, int msg_len);
void hdfixed(int sock_fd, int msg_len);
void hdhealth(int sock_fd, int msg_len);

int targetIds[MAX_PLAYERS];
int activePlayers = 0;
int zombieIds[NUM_ZOMBIES];

void main()
{
    int tcp_socket = socket(AF_INET, SOCK_STREAM, 0); // the file descriptor of listener

    // open network at port 8088 and bind to socket
    struct sockaddr_in addrbuff;
    addrbuff.sin_family = AF_INET;
    addrbuff.sin_addr.s_addr = INADDR_ANY;
    addrbuff.sin_port = htons(PORT); // host to network short

    if (bind(tcp_socket, (struct sockaddr *)&addrbuff, sizeof(struct sockaddr_in)))
    {
        perror("cannot bind");
        exit(1);
    }
    listen(tcp_socket, MAX_CONN_QUEUE); // Can queue up to 512 connections before rejecting
    epoll_events(tcp_socket);
    close(tcp_socket);
}

void epoll_events(int lissock_fd)
{
    char buf[BUFFER_SIZE];
    int n;
    int sock_fd;

    int epoll_fd = epoll_create1(0);
    if (epoll_fd == -1)
    {
        perror("epoll_create1");
        exit(1);
    }

    struct epoll_event con_ev, events[MAX_EVENTS];
    con_ev.events = EPOLLIN; // Interested in input events (REST)
    con_ev.data.fd = lissock_fd;

    if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, lissock_fd, &con_ev))
    {
        perror("epoll_ctl binding listen sock");
        exit(1);
    }

    printf("Successfully Created Listening Socket at fd: %d\n", lissock_fd);

    // Wait for events
    while (1)
    {
        int nfds = epoll_wait(epoll_fd, events, MAX_EVENTS, -1);
        if (nfds == -1)
        {
            perror("epoll_wait");
            exit(1);
        }

        for (int i = 0; i < nfds; i++)
        {
            // Hangle new connection
            if (events[i].data.fd == lissock_fd)
            {
                int connection;
                if ((connection = hdnc(lissock_fd, epoll_fd)) == -1)
                {
                    continue;
                }
                printf("Successfully Created New Connection at fd: %d\n", connection);
            }
            // Handle incoming data
            else
            {
                if (hdnd(events[i].data.fd) != 0)
                {
                    continue;
                }
                //printf("Successfully Handled Data at fd: %d\n", events[i].data.fd);
            }
        }
    }
}

int hdnc(int lissock_fd, int epoll_fd)
{
    struct sockaddr_in client_addr;
    socklen_t client_addr_len = 0;
    int connection;
    if ((connection = accept(lissock_fd, (struct sockaddr *)&client_addr, &client_addr_len)) == -1)
    {
        perror("cannot accept");
        return -1;
    }

    int ctlflags = fcntl(connection, F_GETFL, 0);
    fcntl(connection, F_SETFL, ctlflags | O_NONBLOCK);

    struct epoll_event data_ev;
    data_ev.data.fd = connection;
    data_ev.events = EPOLLIN;

    if (epoll_ctl(epoll_fd, EPOLL_CTL_ADD, connection, &data_ev) == -1)
    {
        perror("epoll_ctl add new connection");
        close(connection);
        return -1;
    }

    return connection;
}

int hdnd(int sock_fd)
{
    // TODO: Handle Game Logic

    // Read the message type and length
    unsigned char msg_type;
    int msg_len;
    read(sock_fd, &msg_type, 1);
    read(sock_fd, &msg_len, 4);

    // Based on message type, handle the message
    switch (msg_type)
    {

    // New Connection
    case 0x01:
        hdroleselection(sock_fd, msg_len);
        break;

    // New Movement
    case 0x02:
        hdmovements(sock_fd, msg_len);
        break;

    // Robot Fixed
    case 0x03:
        hdfixed(sock_fd, msg_len);
        break;

    case 0x04:
        hdhealth(sock_fd, msg_len);
        break;
    }

    return 0;
}
/*

    Role Selection Message

*/
pthread_mutex_t mutex = PTHREAD_MUTEX_INITIALIZER;
int uniqueIdCounter = 0;
void sendRoleResponse(int client_sock, const char *role, int id);
void hdroleselection(int sock_fd, int msg_len)
{
    char *response;
    unsigned char buf;
    read(sock_fd, &buf, 1);
    if (buf == 'R')
    {
        response = "Ruby";
    }
    else if (buf == 'Z')
    {
        response = "Robot";
    }
    else
    {
        return;
    }

    // Generate a unique ID
    pthread_mutex_lock(&mutex);

    uniqueIdCounter++;

    int uniqueId = uniqueIdCounter;

    targetIds[activePlayers] = uniqueId;

    activePlayers++;

    pthread_mutex_unlock(&mutex);

    printf("Assigned ID: %d\n", uniqueId);

    sendRoleResponse(sock_fd, response, uniqueId);
}
void sendRoleResponse(int client_sock, const char *role, int id)
{
    // Calculate the length of the role string
    int len = strlen(role);

    char type = 0x01;

    write(client_sock, &type, 1);

    // Send the length of the string first
    write(client_sock, &len, sizeof(len));

    // Send the id
    write(client_sock, &id, sizeof(id));

    // Then send the actual string
    write(client_sock, role, len);
}


/*

    Postion Update Message

*/

void sendPositionResponse(int client_sock, int id, float X, float Y);
void hdmovements(int sock_fd, int msg_len)
{
    // Assume the robots player only chooses after the first player who has chosen Ruby.
    // The first player's sock fd should be 5, and at this time the activePlayers number is 1.
    // When the robots controller connects at 6, the activePlayers number is 2.

    int originalPlayer = sock_fd - 4;

    float X;
    float Y;

    int Id = 0;
    int isZombie = 0;

    read(sock_fd, &Id, 4);
    if (Id > 30)
    {
        isZombie = 1;
    }
    //printf("Received ID: %d\n", Id);

    read(sock_fd, &X, 4);
    //printf("Received X: %f\n", X);
    read(sock_fd, &Y, 4);
    //printf("Received Y: %f\n", X);

    for (int i = 1; i <= activePlayers; i++)
    {
        if (i == originalPlayer){

            //printf("Skipped: %d\n", originalPlayer);
            continue;
        } // No need to tell position for itself
            
        sendPositionResponse(i + 4, Id, X, Y);
    }
}
void sendPositionResponse(int client_sock, int id, float X, float Y)
{
    // Calculate the length of the role string
    int len = 8;
    char type = 0x02;
    write(client_sock, &type, 1);

    // Send the id
    write(client_sock, &id, sizeof(id));

    // Send X postion
    write(client_sock, &X, sizeof(X));

    // Then Send Y position
    write(client_sock, &Y, sizeof(Y));
}


/*

    Robot Fixed Message

*/

void sendFixedResponse(int client_sock, int fixed_zombie);
void hdfixed(int sock_fd, int msg_len)
{
    int currentPlayer = sock_fd - 4;
    int originalId;
    read(sock_fd, &originalId, 4);
    originalId = originalId - 30;
    for (int i = 1; i <= activePlayers; i++)
    {
        if(i == currentPlayer) continue;
        sendFixedResponse(i + 4, originalId);
    }
}
void sendFixedResponse(int client_sock, int fixed_zombie)
{
    int len = 4; // int32 length
    char type = 0x03;
    write(client_sock, &type, 1);

    // Send the fixed id
    write(client_sock, &fixed_zombie, len);
}


/*

    Role Selection Message

*/
void sendHealthResponse(int client_sock, int Id, int health);
void hdhealth(int sock_fd, int msg_len){
    int currentPlayer, health;
    read(sock_fd, &currentPlayer, 4);    
    read(sock_fd, &health, 4);

    for (int i = 1; i <= activePlayers; i++)
    {
        if(i == currentPlayer) continue;
        sendHealthResponse(i + 4, currentPlayer, health);
    }
}
void sendHealthResponse(int client_sock, int Id, int health){
    int len = 4; // int32 length
    char type = 0x04;
    write(client_sock, &type, 1);

    write(client_sock, &Id, len);

    // Send the fixed id
    write(client_sock, &health, len);
}